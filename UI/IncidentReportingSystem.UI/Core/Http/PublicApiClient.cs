using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace IncidentReportingSystem.UI.Core.Http
{
    public sealed class PublicApiClient : IApiClient
    {
        private readonly HttpClient _client;
        private readonly ILogger<PublicApiClient> _log;

        public PublicApiClient(IHttpClientFactory factory, ILogger<PublicApiClient> log)
        {
            _client = factory.CreateClient("ApiPublic"); 
            _log = log;
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct = default)
            => _client.SendAsync(request, ct);

        public async Task<T?> GetJsonAsync<T>(string path, CancellationToken ct = default)
            => await _client.GetFromJsonAsync<T>(path, ct);

        public async Task<TRes?> PostJsonAsync<TReq, TRes>(string path, TReq body, CancellationToken ct = default)
        {
            using var resp = await _client.PostAsJsonAsync(path, body, ct);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<TRes?>(cancellationToken: ct);
        }

        public async Task PostJsonAsync<TReq>(string path, TReq body, CancellationToken ct = default)
        {
            using var resp = await _client.PostAsJsonAsync(path, body, ct);
            resp.EnsureSuccessStatusCode();
        }

        public async Task PatchJsonAsync<TReq>(string path, TReq body, CancellationToken ct = default)
        {
            using var resp = await _client.PatchAsJsonAsync(path, body, ct);
            resp.EnsureSuccessStatusCode();
        }
    }
}
