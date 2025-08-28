using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace IncidentReportingSystem.IntegrationTests.Utils
{
    /// <summary>
    /// Helper to register a user with roles, login, and return an HttpClient with Bearer token.
    /// Matches tests that call: AuthTestHelpers.RegisterAndLoginAsync(factory, userId: null, roles: new[] {"Admin"})
    /// </summary>
    public static class AuthTestHelpers
    {
        private sealed class LoginResponse
        {
            [JsonPropertyName("accessToken")]
            public string? AccessToken { get; set; }

            [JsonPropertyName("expiresAtUtc")]
            public DateTimeOffset? ExpiresAtUtc { get; set; }
        }

        public static async Task<HttpClient> RegisterAndLoginAsync(
            CustomWebApplicationFactory factory,
            Guid? userId = null,                // kept for signature compatibility; not used
            string[]? roles = null,
            string? email = null,
            string password = "P@ssw0rd!")
        {
            var bootstrap = factory.CreateClient();

            var effectiveEmail = email ?? $"{Guid.NewGuid():N}@example.com";
            var effectiveRoles = (roles is { Length: > 0 }) ? roles : new[] { "User" };

            var regPayload = new { Email = effectiveEmail, Password = password, Roles = effectiveRoles };

            // Try versionless first (as used elsewhere), then explicit /api/v1 as fallback
            var reg = await bootstrap.PostAsJsonAsync(RouteHelper.R(factory, "Auth/register"), regPayload);
            if (!(reg.StatusCode is HttpStatusCode.Created or HttpStatusCode.Conflict))
            {
                reg = await bootstrap.PostAsJsonAsync(RouteHelper.R(factory, "api/v1/Auth/register"), regPayload);
            }
            if (!(reg.StatusCode is HttpStatusCode.Created or HttpStatusCode.Conflict))
                reg.EnsureSuccessStatusCode();

            var loginPayload = new { Email = effectiveEmail, Password = password };
            var login = await bootstrap.PostAsJsonAsync(RouteHelper.R(factory, "Auth/login"), loginPayload);
            if (!login.IsSuccessStatusCode)
            {
                login = await bootstrap.PostAsJsonAsync(RouteHelper.R(factory, "api/v1/Auth/login"), loginPayload);
            }
            login.EnsureSuccessStatusCode();

            string token;
            try
            {
                var lr = await login.Content.ReadFromJsonAsync<LoginResponse>();
                token = lr?.AccessToken ?? string.Empty;
            }
            catch
            {
                token = (await login.Content.ReadAsStringAsync()).Trim('"');
            }

            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("Login did not return a valid access token.");

            var authed = factory.CreateClient();
            authed.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return authed;
        }
    }
}
