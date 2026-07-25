using InsightStream.Core.Entities;

namespace InsightStream.Core.Abstractions;

public interface IScoringService
{
    /// <summary>Returns the cached score for a link, recomputing and caching it if absent or stale.</summary>
    Task<decimal> GetScoreAsync(Link link, CancellationToken cancellationToken = default);

    /// <summary>Computes a fresh 0-10 score for a link without touching the cache.</summary>
    Task<decimal> ComputeAsync(Link link, CancellationToken cancellationToken = default);

    Task InvalidateAsync(Guid linkId, CancellationToken cancellationToken = default);
}
