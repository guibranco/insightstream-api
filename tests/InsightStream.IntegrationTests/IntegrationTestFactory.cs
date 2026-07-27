using InsightStream.Core.Abstractions;
using InsightStream.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace InsightStream.IntegrationTests;

/// <summary>
/// Boots the real API against a Testcontainers-backed Postgres instance. Redis/RabbitMQ-backed
/// services are swapped for in-memory fakes since these tests target the links API against a real
/// relational database, not the messaging/caching infrastructure (covered by unit tests instead).
/// </summary>
public class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Environment variables are read by WebApplication.CreateBuilder as one of the last
        // (highest-precedence) configuration providers, so this reliably overrides
        // appsettings.Development.json's static "ConnectionStrings:Postgres" value — unlike
        // ConfigureAppConfiguration, which for the minimal-hosting model runs too early relative
        // to Program.cs's own CreateBuilder(args) call to take precedence.
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", _postgres.GetConnectionString());

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InsightStreamDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHostedService>();

            services.RemoveAll<IScoreCache>();
            services.AddSingleton<IScoreCache, InMemoryScoreCache>();

            services.RemoveAll<IRateLimiter>();
            services.AddSingleton<IRateLimiter, AlwaysAllowRateLimiter>();
        });
    }
}
