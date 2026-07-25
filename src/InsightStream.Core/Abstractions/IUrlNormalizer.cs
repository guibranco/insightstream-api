namespace InsightStream.Core.Abstractions;

public interface IUrlNormalizer
{
    NormalizedUrl Normalize(string url);
}

public record NormalizedUrl(string Url, string Hash);
