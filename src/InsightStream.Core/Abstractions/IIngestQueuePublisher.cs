using InsightStream.Core.Dtos.Ingest;

namespace InsightStream.Core.Abstractions;

public interface IIngestQueuePublisher
{
    Task PublishAsync(IngestQueueMessage message, CancellationToken cancellationToken = default);

    Task PublishToDlqAsync(IngestQueueMessage message, CancellationToken cancellationToken = default);
}
