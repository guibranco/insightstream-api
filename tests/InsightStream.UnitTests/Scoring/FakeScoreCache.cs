using InsightStream.Core.Abstractions;

namespace InsightStream.UnitTests.Scoring;

internal class FakeScoreCache : IScoreCache
{
    private readonly Dictionary<Guid, decimal> _store = [];

    public int GetCallCount { get; private set; }

    public int SetCallCount { get; private set; }

    public Task<decimal?> GetAsync(Guid linkId, CancellationToken cancellationToken = default)
    {
        GetCallCount++;
        return Task.FromResult(_store.TryGetValue(linkId, out var value) ? value : (decimal?)null);
    }

    public Task SetAsync(
        Guid linkId,
        decimal score,
        TimeSpan ttl,
        CancellationToken cancellationToken = default
    )
    {
        SetCallCount++;
        _store[linkId] = score;
        return Task.CompletedTask;
    }

    public Task InvalidateAsync(Guid linkId, CancellationToken cancellationToken = default)
    {
        _store.Remove(linkId);
        return Task.CompletedTask;
    }
}
