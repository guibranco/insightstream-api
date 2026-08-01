using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace InsightStream.IntegrationTests;

internal static class TestAuth
{
    public static async Task<string> LoginAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { Username = "admin", Password = "ChangeMe123!" }
        );
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data").GetProperty("token").GetString()!;
    }

    public static async Task<HttpClient> CreateAuthenticatedClientAsync(IntegrationTestFactory factory)
    {
        var client = factory.CreateClient();
        var token = await LoginAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
