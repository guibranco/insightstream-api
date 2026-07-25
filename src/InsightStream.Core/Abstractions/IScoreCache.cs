namespace InsightStream.Core.Abstractions;

public interface IScoreCache
{
    Task<decimal?> GetAsync(Guid linkId, CancellationToken cancellationToken = default);

    Task SetAsync(
        Guid linkId,
        decimal score,
        TimeSpan ttl,
        CancellationToken cancellationToken = default
    );

    Task InvalidateAsync(Guid linkId, CancellationToken cancellationToken = default);
}
