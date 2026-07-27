using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using InsightStream.Core.Abstractions;

namespace InsightStream.Infrastructure.Parsing;

public partial class AngleSharpLinkExtractor : ILinkExtractor
{
    private static readonly string[] SocialHosts =
    [
        "facebook.com",
        "twitter.com",
        "x.com",
        "linkedin.com",
        "instagram.com",
        "youtube.com",
        "pinterest.com",
    ];

    private static readonly string[] HeadingTags = ["H1", "H2", "H3", "H4", "H5", "H6"];

    public IReadOnlyList<ExtractedLink> Extract(string html)
    {
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        var results = new List<ExtractedLink>();

        foreach (var anchor in document.QuerySelectorAll("a"))
        {
            var href = anchor.GetAttribute("href");
            if (string.IsNullOrWhiteSpace(href))
            {
                continue;
            }

            if (!Uri.TryCreate(href, UriKind.Absolute, out var uri))
            {
                continue;
            }

            if (uri.Scheme is not ("http" or "https"))
            {
                continue;
            }

            if (ShouldSkip(anchor, uri))
            {
                continue;
            }

            var title = anchor.TextContent.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                title = FindNearestHeadingAncestor(anchor)?.TextContent.Trim() ?? string.Empty;
            }

            var authorName = FindByline(anchor.ParentElement);

            results.Add(new ExtractedLink(href, title, authorName));
        }

        return results;
    }

    private static bool ShouldSkip(IElement anchor, Uri href)
    {
        var current = anchor.ParentElement;
        while (current is not null)
        {
            var className = current.ClassName;
            if (!string.IsNullOrEmpty(className))
            {
                var lower = className.ToLowerInvariant();
                if (
                    lower.Contains("footer")
                    || lower.Contains("social")
                    || lower.Contains("unsubscribe")
                )
                {
                    return true;
                }
            }

            current = current.ParentElement;
        }

        var host = href.Host.ToLowerInvariant();
        if (SocialHosts.Any(h => host == h || host.EndsWith("." + h, StringComparison.Ordinal)))
        {
            return true;
        }

        if (host == "medium.com" || host.EndsWith(".medium.com", StringComparison.Ordinal))
        {
            var path = href.AbsolutePath;
            if (
                path.StartsWith("/m/unsubscribe", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/m/view-in-browser", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/me/settings", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/m/settings", StringComparison.OrdinalIgnoreCase)
                || BareProfilePathRegex().IsMatch(path)
            )
            {
                return true;
            }
        }

        return false;
    }

    private static IElement? FindNearestHeadingAncestor(IElement element)
    {
        var current = element.ParentElement;
        while (current is not null)
        {
            if (HeadingTags.Contains(current.TagName))
            {
                return current;
            }

            current = current.ParentElement;
        }

        return null;
    }

    private static string? FindByline(IElement? block)
    {
        var sibling = block?.NextElementSibling;
        var hops = 0;

        while (sibling is not null && hops < 3)
        {
            var text = sibling.TextContent.Trim();
            var match = BylineRegex().Match(text);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            if (HeadingTags.Contains(sibling.TagName))
            {
                break;
            }

            sibling = sibling.NextElementSibling;
            hops++;
        }

        return null;
    }

    [GeneratedRegex(@"^by\s+(.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex BylineRegex();

    [GeneratedRegex(@"^/@[^/]+/?$")]
    private static partial Regex BareProfilePathRegex();
}
