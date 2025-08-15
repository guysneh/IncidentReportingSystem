using System.Net.Http.Json;

namespace IncidentReportingSystem.IntegrationTests.Utils
{
    public static class HttpClientExtensions
    {
        public static async Task<string> GetJwtTokenAsync(this HttpClient client, string userId = "demo", string role = "Admin")
        {
            var response = await client.GetAsync($"api/{TestConstants.ApiVersion}/Auth/token?userId={userId}&role={role}");
            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadAsStringAsync();
            return token.Trim('"'); 
        }

    }
}
