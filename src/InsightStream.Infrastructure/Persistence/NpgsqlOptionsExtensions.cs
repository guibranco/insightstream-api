using Microsoft.EntityFrameworkCore;

namespace InsightStream.Infrastructure.Persistence;

public static class NpgsqlOptionsExtensions
{
    public static DbContextOptionsBuilder UseInsightStreamPostgres(
        this DbContextOptionsBuilder builder,
        string connectionString
    ) => builder.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
}
