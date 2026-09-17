using InsightStream.Core.Abstractions;

namespace InsightStream.IntegrationTests;

internal class InMemoryScoreCache : IScoreCache
{
    private readonly Dictionary<Guid, decimal> _store = [];

    public Task<decimal?> GetAsync(Guid linkId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.TryGetValue(linkId, out var value) ? value : (decimal?)null);

    public Task SetAsync(
        Guid linkId,
        decimal score,
        TimeSpan ttl,
        CancellationToken cancellationToken = default
    )
    {
        _store[linkId] = score;
        return Task.CompletedTask;
    }

    public Task InvalidateAsync(Guid linkId, CancellationToken cancellationToken = default)
    {
        _store.Remove(linkId);
        return Task.CompletedTask;
    }
}

internal class AlwaysAllowRateLimiter : IRateLimiter
{
    public Task<bool> TryConsumeAsync(
        string key,
        int limit,
        TimeSpan window,
        CancellationToken cancellationToken = default
    ) => Task.FromResult(true);
}
