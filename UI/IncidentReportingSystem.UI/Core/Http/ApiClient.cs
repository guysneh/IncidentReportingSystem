using System.Net.Http.Json;

namespace IncidentReportingSystem.UI.Core.Http
{
    /// <summary>
    /// Thin API client wrapper around HttpClient to centralize base URL usage,
    /// serialization, and optional cross-cutting concerns.
    /// </summary>
    public sealed class ApiClient
    {
        private readonly HttpClient _http;

        /// <summary>Initializes a new instance of <see cref="ApiClient"/>.</summary>
        public ApiClient(HttpClient http) => _http = http ?? throw new ArgumentNullException(nameof(http));

        /// <summary>
        /// Calls <c>GET /auth/me</c> to retrieve the current authenticated user identity.
        /// Returns <c>null</c> if the endpoint yields 401 and the ProblemDetails handler is not applied.
        /// </summary>
        public async Task<WhoAmI?> WhoAmIAsync(CancellationToken cancellationToken = default)
        {
            // Example usage; you can expand this client as the UI grows.
            return await _http.GetFromJsonAsync<WhoAmI>("auth/me", cancellationToken: cancellationToken)
                              .ConfigureAwait(false);
        }

        /// <summary>Minimal shape of the WhoAmI payload consumed by the UI.</summary>
        public sealed record WhoAmI(string userId, string email, string[] roles);
    }
}
