using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InsightStream.Core.Entities;
using InsightStream.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace InsightStream.IntegrationTests;

public class LinksApiTests(IntegrationTestFactory factory) : IClassFixture<IntegrationTestFactory>
{
    private async Task<string> LoginAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { Username = "admin", Password = "ChangeMe123!" }
        );
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data").GetProperty("token").GetString()!;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<Link> SeedLinkAsync(Action<Link>? configure = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InsightStreamDbContext>();

        var link = new Link
        {
            Url = $"https://example.com/{Guid.NewGuid()}",
            UrlHash = Guid.NewGuid().ToString("N"),
            Title = "Integration Test Article",
            FirstSeen = DateTimeOffset.UtcNow,
            LastSeen = DateTimeOffset.UtcNow,
        };
        configure?.Invoke(link);

        db.Links.Add(link);
        await db.SaveChangesAsync();
        return link;
    }

    [Fact]
    public async Task GetLinks_WithoutToken_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/links");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithValidAdminCredentials_ReturnsAToken()
    {
        var client = factory.CreateClient();

        var token = await LoginAsync(client);

        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { Username = "admin", Password = "definitely-wrong" }
        );

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetLinks_WithInvalidStatus_Returns422()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/links?status=NotARealStatus");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetLinks_WithInvalidSort_Returns422()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/links?sort=not_a_real_sort");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetLinkById_ReturnsSeededLinkDetail()
    {
        var link = await SeedLinkAsync();
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/links/{link.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetProperty("title").GetString().Should().Be("Integration Test Article");
        body.GetProperty("data").GetProperty("id").GetString().Should().Be(link.Id.ToString());
    }

    [Fact]
    public async Task GetLinkById_WhenMissing_Returns404()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/links/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateStatus_PersistsTheNewStatus()
    {
        var link = await SeedLinkAsync();
        var client = await CreateAuthenticatedClientAsync();

        var putResponse = await client.PutAsJsonAsync(
            $"/api/links/{link.Id}/status",
            new { Status = "Liked" }
        );
        putResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await client.GetAsync($"/api/links/{link.Id}");
        var body = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetProperty("status").GetString().Should().Be("Liked");
    }

    [Fact]
    public async Task UpdateStatus_WithInvalidEnumValue_Returns400()
    {
        var link = await SeedLinkAsync();
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync(
            $"/api/links/{link.Id}/status",
            new { Status = "NotAStatus" }
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetLinks_FiltersByStatus_AndPaginates()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        for (var i = 0; i < 3; i++)
        {
            await SeedLinkAsync(l =>
            {
                l.Title = $"{marker} Discarded Item {i}";
                l.Status = Core.Enums.LinkStatus.Discarded;
            });
        }

        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync(
            $"/api/links?status=Discarded&search={marker}&per_page=2&page=1&sort=title"
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var data = body.GetProperty("data");
        data.GetArrayLength().Should().Be(2);
        body.GetProperty("pagination").GetProperty("totalItems").GetInt32().Should().Be(3);
    }
}
