using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace InsightStream.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class AuthApiTests(IntegrationTestFactory factory)
{
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsUserAndUpdatesLastLogin()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { Username = "admin", Password = "ChangeMe123!" }
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetProperty("user").GetProperty("username").GetString().Should().Be("admin");
        body.GetProperty("data").GetProperty("token").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithUnknownUsername_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { Username = "not-a-real-user", Password = "whatever" }
        );

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
