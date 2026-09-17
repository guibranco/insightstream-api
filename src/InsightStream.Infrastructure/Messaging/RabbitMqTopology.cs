using RabbitMQ.Client;

namespace InsightStream.Infrastructure.Messaging;

public static class RabbitMqTopology
{
    public static async Task DeclareIngestQueuesAsync(
        this IChannel channel,
        string ingestQueue,
        string ingestDlq,
        CancellationToken cancellationToken = default
    )
    {
        await channel.QueueDeclareAsync(
            queue: ingestQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken
        );

        await channel.QueueDeclareAsync(
            queue: ingestDlq,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken
        );
    }
}
