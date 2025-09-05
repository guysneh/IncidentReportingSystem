using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using IncidentReportingSystem.UI.Core.Auth;

namespace IncidentReportingSystem.UI.Core.Http
{
    public sealed class SecureApiClient : IApiClient
    {
        private readonly HttpClient _client;
        private readonly AuthState _state;
        private readonly ILogger<SecureApiClient> _log;

        public SecureApiClient(IHttpClientFactory factory, AuthState state, ILogger<SecureApiClient> log)
        {
            _client = factory.CreateClient("Api");  
            _state = state;
            _log = log;
        }

        private void AttachBearer(HttpRequestMessage req)
        {
            var tok = _state.AccessToken;
            if (!string.IsNullOrWhiteSpace(tok))
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tok);
                _log.LogDebug("SecureApiClient: +Bearer (len={Len}) -> {Method} {Path}", tok.Length, req.Method, req.RequestUri);
            }
            else
            {
                _log.LogWarning("SecureApiClient: NO token -> {Method} {Path}", req.Method, req.RequestUri);
            }
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct = default)
        {
            AttachBearer(req);
            return await _client.SendAsync(req, ct);
        }

        public async Task PatchJsonAsync<TReq>(string path, TReq body, CancellationToken ct = default)
        {
            var req = new HttpRequestMessage(HttpMethod.Patch, path) { Content = JsonContent.Create(body) };
            AttachBearer(req);
            using var resp = await _client.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();
        }

        public async Task PostJsonAsync<TReq>(string path, TReq body, CancellationToken ct = default)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
            AttachBearer(req);
            using var resp = await _client.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();
        }

        public async Task<T?> GetJsonAsync<T>(string path, CancellationToken ct = default)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, path);
            AttachBearer(req);
            using var resp = await _client.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<T?>(cancellationToken: ct);
        }

        public Task<TRes?> PostJsonAsync<TReq, TRes>(string path, TReq body, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }
}
