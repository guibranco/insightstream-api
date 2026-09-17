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
public class AuthorsApiTests(IntegrationTestFactory factory)
{
    private async Task<Author> SeedAuthorWithLinkAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InsightStreamDbContext>();

        var author = new Author { Name = $"Author {Guid.NewGuid():N}", MediumHandle = $"h{Guid.NewGuid():N}"[..12] };
        var link = new Link
        {
            Url = $"https://example.com/{Guid.NewGuid()}",
            UrlHash = Guid.NewGuid().ToString("N"),
            Title = "Author Test Link",
            FirstSeen = DateTimeOffset.UtcNow,
            LastSeen = DateTimeOffset.UtcNow,
            Status = LinkStatus.Liked,
        };

        db.Authors.Add(author);
        db.Links.Add(link);
        db.LinkAuthors.Add(new LinkAuthor { Author = author, Link = link });
        await db.SaveChangesAsync();

        return author;
    }

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/authors");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_ReturnsAuthorsWithLinkCounts()
    {
        await SeedAuthorWithLinkAsync();
        var client = await TestAuth.CreateAuthenticatedClientAsync(factory);

        var response = await client.GetAsync("/api/authors");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetById_ReturnsAuthorWithInteractionCountsAndLinks()
    {
        var author = await SeedAuthorWithLinkAsync();
        var client = await TestAuth.CreateAuthenticatedClientAsync(factory);

        var response = await client.GetAsync($"/api/authors/{author.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");
        data.GetProperty("name").GetString().Should().Be(author.Name);
        data.GetProperty("links").GetArrayLength().Should().Be(1);
        data.GetProperty("interactionCounts").GetProperty("Liked").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task GetById_WhenMissing_Returns404()
    {
        var client = await TestAuth.CreateAuthenticatedClientAsync(factory);

        var response = await client.GetAsync($"/api/authors/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
