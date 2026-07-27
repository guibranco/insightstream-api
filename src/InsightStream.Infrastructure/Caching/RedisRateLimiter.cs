using InsightStream.Core.Abstractions;
using StackExchange.Redis;

namespace InsightStream.Infrastructure.Caching;

/// <summary>Fixed-window request counter backed by Redis (INCR + EXPIRE on first hit).</summary>
public class RedisRateLimiter(IConnectionMultiplexer redis) : IRateLimiter
{
    public async Task<bool> TryConsumeAsync(
        string key,
        int limit,
        TimeSpan window,
        CancellationToken cancellationToken = default
    )
    {
        var db = redis.GetDatabase();
        var count = await db.StringIncrementAsync(key);
        if (count == 1)
        {
            await db.KeyExpireAsync(key, window);
        }

        return count <= limit;
    }
}
