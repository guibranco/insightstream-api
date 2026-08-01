using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InsightStream.Core.Entities;
using InsightStream.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace InsightStream.IntegrationTests;

public class NewslettersApiTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private async Task<Newsletter> SeedNewsletterWithLinkAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InsightStreamDbContext>();

        var link = new Link
        {
            Url = $"https://example.com/{Guid.NewGuid()}",
            UrlHash = Guid.NewGuid().ToString("N"),
            Title = "Newsletter Test Link",
            FirstSeen = DateTimeOffset.UtcNow,
            LastSeen = DateTimeOffset.UtcNow,
        };

        var newsletter = new Newsletter
        {
            Title = "Newsletter API Test",
            ReceivedDate = DateTimeOffset.UtcNow,
            DestinationEmail = "reader@example.com",
            RawContent = "raw",
            EmailHash = Guid.NewGuid().ToString("N"),
        };

        db.Links.Add(link);
        db.Newsletters.Add(newsletter);
        db.NewsletterLinks.Add(new NewsletterLink { Newsletter = newsletter, Link = link });
        await db.SaveChangesAsync();

        return newsletter;
    }

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/newsletters");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_ReturnsNewslettersWithLinkCounts()
    {
        await SeedNewsletterWithLinkAsync();
        var client = await TestAuth.CreateAuthenticatedClientAsync(factory);

        var response = await client.GetAsync("/api/newsletters");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetById_ReturnsNewsletterWithItsLinks()
    {
        var newsletter = await SeedNewsletterWithLinkAsync();
        var client = await TestAuth.CreateAuthenticatedClientAsync(factory);

        var response = await client.GetAsync($"/api/newsletters/{newsletter.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");
        data.GetProperty("title").GetString().Should().Be("Newsletter API Test");
        data.GetProperty("links").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task GetById_WhenMissing_Returns404()
    {
        var client = await TestAuth.CreateAuthenticatedClientAsync(factory);

        var response = await client.GetAsync($"/api/newsletters/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
