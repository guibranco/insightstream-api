using System.Text.RegularExpressions;
using InsightStream.Core.Abstractions;
using InsightStream.Core.Entities;
using InsightStream.Core.Enums;
using InsightStream.Core.Options;
using InsightStream.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InsightStream.Infrastructure.Scoring;

public partial class ScoringService(InsightStreamDbContext dbContext, IOptions<ScoringOptions> options, IScoreCache cache)
    : IScoringService
{
    private readonly ScoringOptions _options = options.Value;

    public async Task<decimal> GetScoreAsync(Link link, CancellationToken cancellationToken = default)
    {
        var cached = await cache.GetAsync(link.Id, cancellationToken);
        if (cached.HasValue)
        {
            return cached.Value;
        }

        var score = await ComputeAsync(link, cancellationToken);
        await cache.SetAsync(link.Id, score, _options.CacheTtl, cancellationToken);
        return score;
    }

    public async Task<decimal> ComputeAsync(Link link, CancellationToken cancellationToken = default)
    {
        var authorFactor = await ComputeAuthorFactorAsync(link, cancellationToken);
        var keywordFactor = await ComputeKeywordFactorAsync(link, cancellationToken);
        var recencyFactor = ComputeRecencyFactor(link);
        var popularityFactor = await ComputePopularityFactorAsync(link, cancellationToken);

        var raw =
            5.0
            + 5.0 * _options.AuthorWeight * authorFactor
            + 5.0 * _options.KeywordWeight * keywordFactor
            + 5.0 * _options.RecencyWeight * recencyFactor
            + 5.0 * _options.PopularityWeight * popularityFactor;

        var clamped = Math.Clamp(raw, 0.0, 10.0);
        return Math.Round((decimal)clamped, 2, MidpointRounding.AwayFromZero);
    }

    public Task InvalidateAsync(Guid linkId, CancellationToken cancellationToken = default) =>
        cache.InvalidateAsync(linkId, cancellationToken);

    private async Task<double> ComputeAuthorFactorAsync(Link link, CancellationToken cancellationToken)
    {
        var authorIds = link.LinkAuthors.Select(la => la.AuthorId).ToList();
        if (authorIds.Count == 0)
        {
            return 0.0;
        }

        var factors = new List<double>();

        foreach (var authorId in authorIds)
        {
            var counts = await dbContext
                .Links.Where(l => dbContext.LinkAuthors.Any(la => la.AuthorId == authorId && la.LinkId == l.Id))
                .GroupBy(l => l.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var liked = counts.FirstOrDefault(c => c.Status == LinkStatus.Liked)?.Count ?? 0;
            var discarded = counts.FirstOrDefault(c => c.Status == LinkStatus.Discarded)?.Count ?? 0;
            var discardedAfterReview =
                counts.FirstOrDefault(c => c.Status == LinkStatus.DiscardedAfterReview)?.Count ?? 0;

            var total = liked + discarded + discardedAfterReview;
            if (total == 0)
            {
                factors.Add(0.0);
                continue;
            }

            var factor = (liked - discarded - 0.5 * discardedAfterReview) / total;
            factors.Add(Math.Clamp(factor, -1.0, 1.0));
        }

        return factors.Average();
    }

    private async Task<double> ComputeKeywordFactorAsync(Link link, CancellationToken cancellationToken)
    {
        var tokens = Tokenize(link.Title, _options.MinKeywordLength);
        if (tokens.Count == 0)
        {
            return 0.0;
        }

        var weights = await dbContext
            .UserPreferences.Where(p =>
                p.PreferenceType == PreferenceType.Keyword && tokens.Contains(p.PreferenceValue)
            )
            .Select(p => (double)p.Weight)
            .ToListAsync(cancellationToken);

        return weights.Count == 0 ? 0.0 : Math.Clamp(weights.Average(), -1.0, 1.0);
    }

    private double ComputeRecencyFactor(Link link)
    {
        var ageDays = (DateTimeOffset.UtcNow - link.FirstSeen).TotalDays;
        if (_options.RecencyDecayDays <= 0)
        {
            return 0.0;
        }

        return Math.Clamp(1.0 - ageDays / _options.RecencyDecayDays, 0.0, 1.0);
    }

    private async Task<double> ComputePopularityFactorAsync(Link link, CancellationToken cancellationToken)
    {
        var appearanceCount = await dbContext.NewsletterLinks.CountAsync(
            nl => nl.LinkId == link.Id,
            cancellationToken
        );

        if (_options.PopularityCap <= 0)
        {
            return 0.0;
        }

        return Math.Clamp((double)appearanceCount / _options.PopularityCap, 0.0, 1.0);
    }

    internal static List<string> Tokenize(string title, int minLength)
    {
        return TokenRegex()
            .Matches(title.ToLowerInvariant())
            .Select(m => m.Value)
            .Where(t => t.Length >= minLength && !StopWords.English.Contains(t))
            .Distinct()
            .ToList();
    }

    [GeneratedRegex(@"[a-z0-9]+")]
    private static partial Regex TokenRegex();
}
