namespace InsightStream.Core.Options;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public required string HostName { get; set; }

    public int Port { get; set; } = 5672;

    public required string UserName { get; set; }

    public required string Password { get; set; }

    public string VirtualHost { get; set; } = "/";

    public string IngestQueue { get; set; } = "newsletter.ingest";

    public string IngestDlq { get; set; } = "newsletter.ingest.dlq";
}
