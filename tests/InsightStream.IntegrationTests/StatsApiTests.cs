using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InsightStream.Core.Entities;
using InsightStream.Core.Enums;
using InsightStream.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace InsightStream.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class StatsApiTests(IntegrationTestFactory factory)
{
    [Fact]
    public async Task GetStats_WithoutToken_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/stats");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStats_ReturnsCountsAndRecentNewsletters()
    {
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InsightStreamDbContext>();
            db.Newsletters.Add(
                new Newsletter
                {
                    Title = "Stats Test Newsletter",
                    ReceivedDate = DateTimeOffset.UtcNow,
                    DestinationEmail = "reader@example.com",
                    RawContent = "raw",
                    EmailHash = Guid.NewGuid().ToString("N"),
                }
            );
            db.Links.Add(
                new Link
                {
                    Url = $"https://example.com/{Guid.NewGuid()}",
                    UrlHash = Guid.NewGuid().ToString("N"),
                    Title = "Stats Test Link",
                    FirstSeen = DateTimeOffset.UtcNow,
                    LastSeen = DateTimeOffset.UtcNow,
                    Status = LinkStatus.Awaiting,
                }
            );
            await db.SaveChangesAsync();
        }

        var client = await TestAuth.CreateAuthenticatedClientAsync(factory);

        var response = await client.GetAsync("/api/stats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");
        data.GetProperty("totalLinks").GetInt32().Should().BeGreaterThanOrEqualTo(1);
        data.GetProperty("totalNewsletters").GetInt32().Should().BeGreaterThanOrEqualTo(1);
        data.GetProperty("statusCounts").TryGetProperty("Awaiting", out _).Should().BeTrue();
        data.GetProperty("recentNewsletters").GetArrayLength().Should().BeLessThanOrEqualTo(5);
    }
}
