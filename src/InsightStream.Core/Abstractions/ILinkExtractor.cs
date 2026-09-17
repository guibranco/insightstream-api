namespace InsightStream.Core.Abstractions;

public interface ILinkExtractor
{
    IReadOnlyList<ExtractedLink> Extract(string html);
}

/// <summary>
/// A candidate article link found in a newsletter body. <see cref="Url"/> is the raw, un-normalized
/// href; author handle/custom-domain attribution is derived later from the normalized URL.
/// </summary>
public record ExtractedLink(string Url, string Title, string? AuthorName);
