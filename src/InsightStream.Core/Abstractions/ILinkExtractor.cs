namespace InsightStream.Core.Abstractions;

public interface ILinkExtractor
{
    IReadOnlyList<ExtractedLink> Extract(string html);
}

public record ExtractedLink(
    string Url,
    string Title,
    string? AuthorName,
    string? AuthorHandle,
    string? AuthorDomain
);
