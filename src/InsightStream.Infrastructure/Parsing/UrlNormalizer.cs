using System.Security.Cryptography;
using System.Text;
using InsightStream.Core.Abstractions;

namespace InsightStream.Infrastructure.Parsing;

public class UrlNormalizer : IUrlNormalizer
{
    public NormalizedUrl Normalize(string url)
    {
        var normalized = NormalizeInternal(url, redirectHops: 0);
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
        return new NormalizedUrl(normalized, hash);
    }

    private static string NormalizeInternal(string url, int redirectHops)
    {
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return url.Trim();
        }

        if (redirectHops < 5 && IsMediumRedirect(uri))
        {
            var innerUrl = ExtractRedirectTarget(uri);
            if (innerUrl is not null)
            {
                return NormalizeInternal(innerUrl, redirectHops + 1);
            }
        }

        var remainingQuery = QueryHelpers
            .ParseQuery(uri.Query)
            .Where(kv => !IsStrippedParam(kv.Key))
            .ToList();

        var path = uri.AbsolutePath;
        if (path.Length > 1 && path.EndsWith('/'))
        {
            path = path.TrimEnd('/');
        }

        var builder = new UriBuilder
        {
            Scheme = uri.Scheme.ToLowerInvariant(),
            Host = uri.Host.ToLowerInvariant(),
            Port = uri.IsDefaultPort ? -1 : uri.Port,
            Path = path,
            Query = remainingQuery.Count == 0 ? string.Empty : QueryHelpers.BuildQuery(remainingQuery),
            Fragment = string.Empty,
        };

        var result = builder.Uri.GetComponents(
            UriComponents.SchemeAndServer | UriComponents.PathAndQuery,
            UriFormat.SafeUnescaped
        );

        return result;
    }

    private static bool IsMediumRedirect(Uri uri) =>
        uri.Host.Equals("medium.com", StringComparison.OrdinalIgnoreCase)
        && uri.AbsolutePath.Equals("/m/global-redirect", StringComparison.OrdinalIgnoreCase);

    private static string? ExtractRedirectTarget(Uri uri)
    {
        var query = QueryHelpers.ParseQuery(uri.Query);
        return query.FirstOrDefault(kv => kv.Key.Equals("url", StringComparison.OrdinalIgnoreCase)).Value;
    }

    private static bool IsStrippedParam(string key) =>
        key.StartsWith("utm_", StringComparison.OrdinalIgnoreCase)
        || key.Equals("source", StringComparison.OrdinalIgnoreCase);
}

internal static class QueryHelpers
{
    public static List<KeyValuePair<string, string>> ParseQuery(string query)
    {
        var result = new List<KeyValuePair<string, string>>();
        if (string.IsNullOrEmpty(query))
        {
            return result;
        }

        var trimmed = query.TrimStart('?');
        foreach (var pair in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
            result.Add(new KeyValuePair<string, string>(key, value));
        }

        return result;
    }

    public static string BuildQuery(IReadOnlyList<KeyValuePair<string, string>> query)
    {
        var parts = query.Select(kv =>
            string.IsNullOrEmpty(kv.Value)
                ? Uri.EscapeDataString(kv.Key)
                : $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"
        );

        return "?" + string.Join('&', parts);
    }
}
