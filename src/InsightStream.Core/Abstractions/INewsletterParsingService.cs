namespace InsightStream.Core.Abstractions;

/// <summary>
/// Combines <see cref="IEmailParser"/>, <see cref="ILinkExtractor"/>, and <see cref="IUrlNormalizer"/>
/// into a single pass over a raw newsletter email: parse MIME, extract candidate article links,
/// normalize their URLs, derive author attribution from the normalized URL, and de-duplicate links
/// that appear more than once within the same email.
/// </summary>
public interface INewsletterParsingService
{
    ParsedNewsletter Parse(byte[] rawEmail);
}

public record ParsedNewsletter(
    string Subject,
    string DestinationEmail,
    DateTimeOffset Date,
    IReadOnlyList<ProcessedLink> Links
);

public record ProcessedLink(
    string Url,
    string UrlHash,
    string Title,
    string? AuthorName,
    string? AuthorHandle,
    string? AuthorDomain
);
