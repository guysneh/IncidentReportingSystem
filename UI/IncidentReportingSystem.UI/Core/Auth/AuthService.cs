using IncidentReportingSystem.UI.Core.Auth;
using IncidentReportingSystem.UI.Core.Options;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using System.Net.Http.Headers;
using System.Net.Http.Json;

public sealed class AuthService : IAuthService
{
    private readonly HttpClient _public; // בלי Authorization
    private readonly HttpClient _api;    // עם Authorization (AuthHeaderHandler)
    private readonly IJSRuntime _js;
    private readonly AuthState _state;

    public AuthService(IHttpClientFactory factory, IJSRuntime js, AuthState state)
    {
        _public = factory.CreateClient("ApiPublic");
        _api = factory.CreateClient("Api");
        _js = js; _state = state;
    }

    private sealed class LoginResponse
    {
        public string? AccessToken { get; set; }
        public DateTimeOffset? ExpiresAtUtc { get; set; }
    }

    public async Task<bool> SignInAsync(string email, string password, CancellationToken ct = default)
    {
        // IMPORTANT: relative paths, baseUrl already includes /api/v1/
        var resp = await _public.PostAsJsonAsync("auth/login", new { email, password }, ct);
        if (!resp.IsSuccessStatusCode) return false;

        var dto = await resp.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: ct);
        if (dto?.AccessToken is null) return false;
        var expJwt = JwtExpUtc(dto.AccessToken);
        Console.WriteLine($"[LOGIN] jwtExp={expJwt:O}, apiExp={dto.ExpiresAtUtc:O}, now={DateTimeOffset.UtcNow:O}");

        // persist token to localStorage (object with token + exp)
        await _js.InvokeVoidAsync("irsAuth.set", dto.AccessToken, dto.ExpiresAtUtc);

        // update in-memory state so subsequent calls attach Bearer
        await _state.SetAsync(dto.AccessToken);
        return true;
    }

    public async Task RegisterAsync(string email, string password, string role, string first, string last, CancellationToken ct = default)
    {
        var payload = new
        {
            email,
            password,
            roles = new[] { string.IsNullOrWhiteSpace(role) ? "User" : role },
            firstName = first,
            lastName = last
        };

        var resp = await _public.PostAsJsonAsync("auth/register", payload, ct); // ← relative
        resp.EnsureSuccessStatusCode();

        var dto = await resp.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: ct);
        if (!string.IsNullOrWhiteSpace(dto?.AccessToken))
        {
            await _js.InvokeVoidAsync("irsAuth.set", dto!.AccessToken!, dto!.ExpiresAtUtc);
            await _state.SetAsync(dto!.AccessToken!);
        }
    }

    public async Task<AuthModels.WhoAmI?> MeAsync(CancellationToken ct = default)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "auth/me");

        var tok = _state.AccessToken;
        if (!string.IsNullOrWhiteSpace(tok))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tok);

        Console.WriteLine($"[ME] sending Authorization={(tok is { Length: > 0 })} len={tok?.Length ?? 0}");

        var resp = await _api.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var authHdr = resp.Headers.WwwAuthenticate.FirstOrDefault()?.ToString() ?? "-";
            var body = await resp.Content.ReadAsStringAsync(ct);
            Console.WriteLine($"[ME] {(int)resp.StatusCode} (WWW-Authenticate={authHdr}) body={body}");
            return null;
        }

        return await resp.Content.ReadFromJsonAsync<AuthModels.WhoAmI?>(cancellationToken: ct);
    }

    public Task UpdateMeAsync(string first, string last, CancellationToken ct = default)
        => _api.PatchAsJsonAsync("auth/me", new { firstName = first, lastName = last }, ct);

    public Task ChangePasswordAsync(string current, string @new, CancellationToken ct = default)
        => _api.PostAsJsonAsync("auth/me/change-password", new { currentPassword = current, newPassword = @new }, ct);

    public async Task SignOutAsync(CancellationToken ct = default)
    {
        try { await _js.InvokeVoidAsync("irsAuth.clear"); } catch { }
        await _state.ClearAsync();
    }

    private static DateTimeOffset? JwtExpUtc(string? jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt) || jwt.Count(c => c == '.') != 2) return null;
        try
        {
            var payload = jwt.Split('.')[1];
            var padded = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(padded));
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("exp", out var expEl)) return null;
            return DateTimeOffset.FromUnixTimeSeconds(expEl.GetInt64()).ToUniversalTime();
        }
        catch { return null; }
    }

}
