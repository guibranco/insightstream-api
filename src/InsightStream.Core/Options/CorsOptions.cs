namespace InsightStream.Core.Options;

public class CorsOptions
{
    public const string SectionName = "Cors";

    public const string PolicyName = "Frontend";

    public required string AllowedOrigin { get; set; }
}
