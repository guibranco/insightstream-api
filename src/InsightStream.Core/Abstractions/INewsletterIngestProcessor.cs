using InsightStream.Core.Dtos.Ingest;

namespace InsightStream.Core.Abstractions;

/// <summary>
/// Parses a raw newsletter email and persists it (newsletter, links, authors, join rows) inside a
/// single transaction. Idempotent: re-processing a message for an already-persisted email hash is a
/// no-op.
/// </summary>
public interface INewsletterIngestProcessor
{
    Task ProcessAsync(IngestQueueMessage message, CancellationToken cancellationToken = default);
}
