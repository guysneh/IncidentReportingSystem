using Azure.Core;
using IncidentReportingSystem.UI.Core.Auth;
using MediatR;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using static System.Net.WebRequestMethods;

namespace IncidentReportingSystem.UI.Core.Http;

public sealed class SecureApiClient : IApiClient
{
    private readonly HttpClient _client;
    private readonly AuthState _auth;

    public SecureApiClient(IHttpClientFactory httpClientFactory, AuthState auth)
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
        if (!resp.IsSuccessStatusCode)
            throw await ApiErrorException.FromResponseAsync(resp, ct);

        if (resp.StatusCode == System.Net.HttpStatusCode.NoContent) return default;
        return await resp.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
    }

    public async Task PostJsonAsync<TReq>(string path, TReq body, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, path)
        { Content = JsonContent.Create(body) };
        AttachBearer(req);
        using var resp = await _client.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
            throw await ApiErrorException.FromResponseAsync(resp, ct);
    }

    public async Task<TRes?> PostJsonAsync<TReq, TRes>(string path, TReq body, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, path)
        { Content = JsonContent.Create(body) };
        AttachBearer(req);
        using var resp = await _client.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
            throw await ApiErrorException.FromResponseAsync(resp, ct);

        if (resp.StatusCode == System.Net.HttpStatusCode.NoContent) return default;
        return await resp.Content.ReadFromJsonAsync<TRes?>(cancellationToken: ct);
    }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct = default)
    {
        AttachBearer(request);
        return await _client.SendAsync(request, ct);
    }

    public async Task PatchJsonAsync<TReq>(string path, TReq body, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Put, path)
        { Content = JsonContent.Create(body) };
        AttachBearer(req);
        using var resp = await _client.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
            throw await ApiErrorException.FromResponseAsync(resp, ct);   
    }

    public async Task DeleteAsync(string path, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Delete, path);
        AttachBearer(req);
        var res = await _client.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
    }
}
