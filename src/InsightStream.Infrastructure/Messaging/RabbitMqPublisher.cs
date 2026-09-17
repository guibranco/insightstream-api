using System.Text.Json;
using InsightStream.Core.Abstractions;
using InsightStream.Core.Dtos.Ingest;
using InsightStream.Core.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace InsightStream.Infrastructure.Messaging;

public class RabbitMqPublisher(RabbitMqConnection connection, IOptions<RabbitMqOptions> options)
    : IIngestQueuePublisher
{
    private readonly RabbitMqOptions _options = options.Value;

    public Task PublishAsync(IngestQueueMessage message, CancellationToken cancellationToken = default) =>
        PublishToAsync(_options.IngestQueue, message, cancellationToken);

    public Task PublishToDlqAsync(IngestQueueMessage message, CancellationToken cancellationToken = default) =>
        PublishToAsync(_options.IngestDlq, message, cancellationToken);

    private async Task PublishToAsync(
        string queue,
        IngestQueueMessage message,
        CancellationToken cancellationToken
    )
    {
        var conn = await connection.GetConnectionAsync(cancellationToken);
        await using var channel = await conn.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.DeclareIngestQueuesAsync(_options.IngestQueue, _options.IngestDlq, cancellationToken);

        var body = JsonSerializer.SerializeToUtf8Bytes(message);

        var properties = new BasicProperties { Persistent = true, ContentType = "application/json" };

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queue,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken
        );
    }
}
