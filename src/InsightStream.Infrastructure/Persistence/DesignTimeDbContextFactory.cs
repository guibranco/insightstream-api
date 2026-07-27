using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace InsightStream.Infrastructure.Persistence;

/// <summary>
/// Used only by `dotnet ef migrations add` / `dotnet ef database update` at design time. The
/// connection string here is never used at runtime — the app wires up <see cref="InsightStreamDbContext"/>
/// itself in Program.cs using the real configuration.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<InsightStreamDbContext>
{
    public InsightStreamDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("INSIGHTSTREAM_DESIGN_TIME_CONNECTION")
            ?? "Host=localhost;Database=insightstream;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<InsightStreamDbContext>();
        optionsBuilder.UseInsightStreamPostgres(connectionString);

        return new InsightStreamDbContext(optionsBuilder.Options);
    }
}
