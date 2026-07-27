using InsightStream.Core.Abstractions;
using InsightStream.Core.Entities;
using InsightStream.Core.Enums;
using InsightStream.Core.Options;
using InsightStream.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InsightStream.Infrastructure.Scoring;

public class PreferenceLearningService(InsightStreamDbContext dbContext, IOptions<ScoringOptions> options)
    : IPreferenceLearningService
{
    private readonly ScoringOptions _options = options.Value;

    public async Task ApplyAsync(
        Guid userId,
        Link link,
        LinkStatus newStatus,
        CancellationToken cancellationToken = default
    )
    {
        var authorDelta = newStatus switch
        {
            LinkStatus.Liked => _options.AuthorLikeAdjustment,
            LinkStatus.DiscardedAfterReview => _options.AuthorReviewDiscardAdjustment,
            LinkStatus.Discarded => _options.AuthorDiscardAdjustment,
            _ => (decimal?)null,
        };

        if (authorDelta is null)
        {
            // Reverting to Awaiting (or any other non-terminal status) carries no learning signal.
            return;
        }

        var keywordDelta = authorDelta.Value * 0.5m;

        foreach (var authorIdentity in await ResolveAuthorIdentitiesAsync(link, cancellationToken))
        {
            await AdjustPreferenceAsync(
                userId,
                PreferenceType.Author,
                authorIdentity,
                authorDelta.Value,
                cancellationToken
            );
        }

        foreach (var token in ScoringService.Tokenize(link.Title, _options.MinKeywordLength))
        {
            await AdjustPreferenceAsync(userId, PreferenceType.Keyword, token, keywordDelta, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<string>> ResolveAuthorIdentitiesAsync(Link link, CancellationToken cancellationToken)
    {
        var authorIds = link.LinkAuthors.Select(la => la.AuthorId).ToList();
        if (authorIds.Count == 0)
        {
            return [];
        }

        return await dbContext
            .Authors.Where(a => authorIds.Contains(a.Id))
            .Select(a => a.MediumHandle ?? a.CustomDomain ?? a.Name)
            .ToListAsync(cancellationToken);
    }

    private async Task AdjustPreferenceAsync(
        Guid userId,
        PreferenceType type,
        string value,
        decimal delta,
        CancellationToken cancellationToken
    )
    {
        var preference = await dbContext.UserPreferences.FirstOrDefaultAsync(
            p => p.UserId == userId && p.PreferenceType == type && p.PreferenceValue == value,
            cancellationToken
        );

        if (preference is null)
        {
            dbContext.UserPreferences.Add(
                new UserPreference
                {
                    UserId = userId,
                    PreferenceType = type,
                    PreferenceValue = value,
                    Weight = Math.Clamp(delta, -1.00m, 1.00m),
                }
            );
        }
        else
        {
            preference.Weight = Math.Clamp(preference.Weight + delta, -1.00m, 1.00m);
        }
    }
}
