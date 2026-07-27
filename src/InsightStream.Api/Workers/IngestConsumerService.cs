using System.Text.Json;
using InsightStream.Core.Abstractions;
using InsightStream.Core.Dtos.Ingest;
using InsightStream.Core.Options;
using InsightStream.Infrastructure.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace InsightStream.Api.Workers;

/// <summary>
/// Consumes the "newsletter.ingest" queue with manual ack and prefetch 1. On processing failure the
/// message is republished with an incremented retry count (up to <see cref="IngestOptions.MaxRetries"/>)
/// or routed to the dead-letter queue, and the original delivery is acked either way.
/// </summary>
public class IngestConsumerService(
    RabbitMqConnection connection,
    IIngestQueuePublisher publisher,
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> rabbitMqOptions,
    IOptions<IngestOptions> ingestOptions,
    ILogger<IngestConsumerService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = rabbitMqOptions.Value;
        var conn = await connection.GetConnectionAsync(stoppingToken);
        var channel = await conn.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.DeclareIngestQueuesAsync(options.IngestQueue, options.IngestDlq, stoppingToken);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, ea) => HandleMessageAsync(channel, ea, stoppingToken);

        await channel.BasicConsumeAsync(
            queue: options.IngestQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken
        );

        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { }, TaskScheduler.Default);
    }

    private async Task HandleMessageAsync(
        IChannel channel,
        BasicDeliverEventArgs ea,
        CancellationToken stoppingToken
    )
    {
        IngestQueueMessage? message = null;

        try
        {
            message = JsonSerializer.Deserialize<IngestQueueMessage>(ea.Body.Span);
            if (message is null)
            {
                throw new InvalidOperationException("Ingest message payload deserialized to null.");
            }

            using var scope = scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<INewsletterIngestProcessor>();
            await processor.ProcessAsync(message, stoppingToken);

            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to process ingest message for EmailHash {EmailHash} (retry {RetryCount})",
                message?.EmailHash,
                message?.RetryCount
            );

            await HandleFailureAsync(message, ea, channel, stoppingToken);
        }
    }

    private async Task HandleFailureAsync(
        IngestQueueMessage? message,
        BasicDeliverEventArgs ea,
        IChannel channel,
        CancellationToken stoppingToken
    )
    {
        if (message is not null)
        {
            if (message.RetryCount < ingestOptions.Value.MaxRetries)
            {
                await publisher.PublishAsync(
                    message with { RetryCount = message.RetryCount + 1 },
                    stoppingToken
                );
            }
            else
            {
                logger.LogError(
                    "Ingest message for EmailHash {EmailHash} exceeded max retries, routing to DLQ",
                    message.EmailHash
                );
                await publisher.PublishToDlqAsync(message, stoppingToken);
            }
        }

        // Ack the original delivery either way: a replacement message (retry or DLQ) has already
        // been published, so leaving this one un-acked would just duplicate the work.
        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, stoppingToken);
    }
}
