namespace InsightStream.Core.Abstractions;

public interface IRateLimiter
{
    /// <summary>
    /// Atomically increments the counter for <paramref name="key"/> and returns whether the request
    /// is still within <paramref name="limit"/> for the sliding <paramref name="window"/>.
    /// </summary>
    Task<bool> TryConsumeAsync(
        string key,
        int limit,
        TimeSpan window,
        CancellationToken cancellationToken = default
    );
}
