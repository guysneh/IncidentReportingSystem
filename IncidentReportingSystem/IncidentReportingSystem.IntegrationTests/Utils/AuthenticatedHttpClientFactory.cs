using System.Net.Http.Headers;

namespace IncidentReportingSystem.IntegrationTests.Utils;

public static class AuthenticatedHttpClientFactory
{
    public static async Task<HttpClient> CreateClientWithTokenAsync(CustomWebApplicationFactory factory, string userId = "demo", string role = "Admin")
    {
        var client = factory.CreateClient();

        var tokenResponse = await client.GetAsync($"/api/v1/auth/token?userId={userId}&role={role}");
        tokenResponse.EnsureSuccessStatusCode();

        var token = await tokenResponse.Content.ReadAsStringAsync();

        var authClient = factory.CreateClient();
        authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim('"'));

        return authClient;
    }
}
