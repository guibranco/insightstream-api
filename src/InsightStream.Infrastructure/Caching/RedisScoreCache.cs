using System.Globalization;
using InsightStream.Core.Abstractions;
using StackExchange.Redis;

namespace InsightStream.Infrastructure.Caching;

public class RedisScoreCache(IConnectionMultiplexer redis) : IScoreCache
{
    private static string KeyFor(Guid linkId) => $"link:score:{linkId}";

    public async Task<decimal?> GetAsync(Guid linkId, CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        var value = await db.StringGetAsync(KeyFor(linkId));
        return value.HasValue ? decimal.Parse(value.ToString(), CultureInfo.InvariantCulture) : null;
    }

    public async Task SetAsync(
        Guid linkId,
        decimal score,
        TimeSpan ttl,
        CancellationToken cancellationToken = default
    )
    {
        var db = redis.GetDatabase();
        await db.StringSetAsync(KeyFor(linkId), score.ToString(CultureInfo.InvariantCulture), ttl);
    }

    public async Task InvalidateAsync(Guid linkId, CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        await db.KeyDeleteAsync(KeyFor(linkId));
    }
}
