using System.Text.RegularExpressions;
using InsightStream.Core.Abstractions;

namespace InsightStream.Infrastructure.Parsing;

public partial class NewsletterParsingService(
    IEmailParser emailParser,
    ILinkExtractor linkExtractor,
    IUrlNormalizer urlNormalizer
) : INewsletterParsingService
{
    public ParsedNewsletter Parse(byte[] rawEmail)
    {
        var email = emailParser.Parse(rawEmail);
        var candidates = linkExtractor.Extract(email.HtmlBody);

        var seen = new Dictionary<string, ProcessedLink>();

        foreach (var candidate in candidates)
        {
            var normalized = urlNormalizer.Normalize(candidate.Url);
            if (seen.ContainsKey(normalized.Hash))
            {
                continue;
            }

            var (handle, domain) = ResolveAuthorAttribution(normalized.Url);
            var title = string.IsNullOrWhiteSpace(candidate.Title)
                ? FallbackTitle(normalized.Url)
                : candidate.Title;

            seen[normalized.Hash] = new ProcessedLink(
                Url: normalized.Url,
                UrlHash: normalized.Hash,
                Title: title,
                AuthorName: candidate.AuthorName,
                AuthorHandle: handle,
                AuthorDomain: domain
            );
        }

        return new ParsedNewsletter(email.Subject, email.DestinationEmail, email.Date, [.. seen.Values]);
    }

    private static (string? Handle, string? Domain) ResolveAuthorAttribution(string normalizedUrl)
    {
        if (!Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var uri))
        {
            return (null, null);
        }

        var host = uri.Host.ToLowerInvariant();

        if (host == "medium.com" || host.EndsWith(".medium.com", StringComparison.Ordinal))
        {
            var match = MediumHandleRegex().Match(uri.AbsolutePath);
            return match.Success ? (match.Groups[1].Value, null) : (null, null);
        }

        return (null, host);
    }

    private static string FallbackTitle(string normalizedUrl)
    {
        if (!Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var uri))
        {
            return normalizedUrl;
        }

        var lastSegment = uri.AbsolutePath.Trim('/').Split('/').LastOrDefault() ?? uri.Host;
        return lastSegment.Replace('-', ' ').Replace('_', ' ').Trim();
    }

    [GeneratedRegex(@"^/@([^/]+)/")]
    private static partial Regex MediumHandleRegex();
}
