using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace IncidentReportingSystem.UI.Core.Auth
{
    public sealed class AuthService : IAuthService
    {
        private readonly HttpClient _api;   // Protected (adds Bearer via AuthHeaderHandler)
        private readonly HttpClient _pub;   // Public (login/register)
        private readonly AuthState _state;
        private readonly IJSRuntime _js;

        public AuthService(IHttpClientFactory httpFactory, AuthState state, IJSRuntime js)
        {
            _api = httpFactory.CreateClient("Api");        
            _pub = httpFactory.CreateClient("ApiPublic");  
            _state = state;
            _js = js;
        }

        private sealed record TokenDto(string? AccessToken, System.DateTimeOffset? ExpiresAtUtc);

        // ---------- API calls ----------
        public async Task<bool> SignInAsync(string email, string password, CancellationToken ct = default)
        {
            var resp = await _pub.PostAsJsonAsync("auth/login", new { email, password }, ct);
            if (!resp.IsSuccessStatusCode) return false;

            var dto = await resp.Content.ReadFromJsonAsync<TokenDto>(cancellationToken: ct);
            if (string.IsNullOrWhiteSpace(dto?.AccessToken)) return false;

            // 1) persist to localStorage
            try { await _js.InvokeVoidAsync("irsAuth.set", dto.AccessToken, dto.ExpiresAtUtc); } catch { }

            // 2) update in-memory state
            await _state.SetAsync(dto.AccessToken!, dto.ExpiresAtUtc);

            // 3) give the pipeline a tick
            await Task.Yield();

            return true;
        }

        public async Task<bool> RegisterAsync(
            string email, string password, string role, string firstName, string lastName, CancellationToken ct = default)
        {
            var body = new { email, password, roles = new[] { role }, firstName, lastName };
            var resp = await _pub.PostAsJsonAsync("auth/register", body, ct);
            if (!resp.IsSuccessStatusCode) return false;

            TokenDto? dto = null;
            try { dto = await resp.Content.ReadFromJsonAsync<TokenDto>(cancellationToken: ct); } catch { }

            if (string.IsNullOrWhiteSpace(dto?.AccessToken))
                return await SignInAsync(email, password, ct);

            await _state.SetAsync(dto!.AccessToken!, dto.ExpiresAtUtc);
            try { await _js.InvokeVoidAsync("irsAuth.set", dto.AccessToken, dto.ExpiresAtUtc); } catch { }
            return true;
        }

        public async Task<AuthModels.WhoAmI?> MeAsync(CancellationToken ct = default)
        {
            // Build request explicitly and attach Authorization from AuthState
            using var req = new HttpRequestMessage(HttpMethod.Get, "auth/me");

            var token = _state.AccessToken;
            if (!string.IsNullOrWhiteSpace(token))
            {
                req.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            // IMPORTANT: use the protected client (_api) that points to .../api/v1/
            var resp = await _api.SendAsync(req, ct);

            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return null;

            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadFromJsonAsync<AuthModels.WhoAmI>(cancellationToken: ct);
        }

        public async Task<bool> UpdateMeAsync(string firstName, string lastName, CancellationToken ct = default)
        {
            var resp = await _api.PatchAsJsonAsync("auth/me", new { firstName, lastName }, ct);
            return resp.IsSuccessStatusCode;
        }

        public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct = default)
        {
            var resp = await _api.PostAsJsonAsync("auth/me/change-password",
                new { currentPassword, newPassword }, ct);
            return resp.IsSuccessStatusCode;
        }

        public async Task SignOutAsync(CancellationToken ct = default)
        {
            await _state.ClearAsync();
            try { await _js.InvokeVoidAsync("irsAuth.clear"); } catch { }
        }
    }
}
