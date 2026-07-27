namespace InsightStream.Core.Dtos.Ingest;

/// <summary>Payload published to (and consumed from) the "newsletter.ingest" queue.</summary>
public record IngestQueueMessage
{
    public required string EmailHash { get; init; }

    public required string RawEmailBase64 { get; init; }

    public int RetryCount { get; init; }
}
