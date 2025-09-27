using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using IncidentReportingSystem.UI.Core.Http;

namespace IncidentReportingSystem.UI.Core.Users
{
    public interface IUserDirectory
    {
        Task<IDictionary<string, string>> ResolveDisplayNamesAsync(IEnumerable<string> userIds, CancellationToken ct = default);
    }

    public sealed class UserDirectory : IUserDirectory
    {
        private readonly IApiClient _api;
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        public UserDirectory(IApiClient api) => _api = api;

        public async Task<IDictionary<string, string>> ResolveDisplayNamesAsync(IEnumerable<string> userIds, CancellationToken ct = default)
        {
            var ids = (userIds ?? Array.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (ids.Length == 0)
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            using var req = new HttpRequestMessage(HttpMethod.Post, "Auth/resolve-display-names")
            {
                Content = JsonContent.Create(new { ids })
            };
            using var resp = await _api.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();

            var payload = await resp.Content.ReadAsStringAsync(ct);
            var map = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(payload, Json)
                      ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            return new Dictionary<string, string>(map, StringComparer.OrdinalIgnoreCase);
        }
    }
}
