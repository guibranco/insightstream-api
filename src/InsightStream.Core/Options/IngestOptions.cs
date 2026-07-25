namespace InsightStream.Core.Options;

public class IngestOptions
{
    public const string SectionName = "Ingest";

    public required string Token { get; set; }

    public long MaxRequestBodyBytes { get; set; } = 10 * 1024 * 1024;

    public int RateLimitPerHour { get; set; } = 60;

    public int MaxRetries { get; set; } = 3;
}
