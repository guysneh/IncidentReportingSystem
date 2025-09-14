using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace IncidentReportingSystem.UI.Core.Http;

public sealed class SecureApiClient : IApiClient
{
    private readonly HttpClient _client;
    private readonly Auth.AuthState _auth;

    public SecureApiClient(IHttpClientFactory httpClientFactory, Auth.AuthState auth)
    {
        _client = httpClientFactory.CreateClient("Api");
        _auth = auth;
    }

    private void AttachBearer(HttpRequestMessage req)
    {
        var token = _auth.AccessToken;
        if (!string.IsNullOrWhiteSpace(token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<T?> GetJsonAsync<T>(string path, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, path);
        AttachBearer(req);
        using var resp = await _client.SendAsync(req, ct);
        if (resp.StatusCode == System.Net.HttpStatusCode.NoContent) return default;
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
    }

    public async Task PostJsonAsync<TReq>(string path, TReq body, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        AttachBearer(req);
        using var resp = await _client.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<TRes?> PostJsonAsync<TReq, TRes>(string path, TReq body, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        AttachBearer(req);
        using var resp = await _client.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<TRes?>(cancellationToken: ct);
    }

    public async Task PatchJsonAsync<TReq>(string path, TReq body, CancellationToken ct = default)
    {
        var method = new HttpMethod("PATCH");
        using var req = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body) };
        AttachBearer(req);
        using var resp = await _client.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
    }

    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct = default)
    {
        AttachBearer(request);
        return _client.SendAsync(request, ct);
    }
}
