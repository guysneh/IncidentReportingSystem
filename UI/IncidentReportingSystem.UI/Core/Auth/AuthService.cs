using System;
using System.Threading;
using System.Threading.Tasks;
using IncidentReportingSystem.UI.Core.Auth;
using IncidentReportingSystem.UI.Core.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using static IncidentReportingSystem.UI.Core.Auth.AuthModels;

namespace IncidentReportingSystem.UI.Core.Auth
{
    public sealed class AuthService : IAuthService
    {
        private readonly PublicApiClient _publicApi;
        private readonly SecureApiClient _secureApi;
        private readonly IJSRuntime _js;
        private readonly AuthState _state;
        private readonly ILogger<AuthService> _log;
        private readonly NavigationManager _nav;

        public AuthService(
            PublicApiClient publicApi,
            SecureApiClient secureApi,
            IJSRuntime js,
            AuthState state,
            ILogger<AuthService> log,
            NavigationManager nav)
        {
            _publicApi = publicApi;
            _secureApi = secureApi;
            _js = js;
            _state = state;
            _log = log;
            _nav = nav;
        }

        public async Task<bool> SignInAsync(string email, string password, CancellationToken ct = default)
        {
            try
            {
                var dto = await SignInRawAsync(email, password, ct);
                if (dto is null || string.IsNullOrWhiteSpace(dto.AccessToken))
                    return false;

                await _js.InvokeVoidAsync("irsAuth.set", dto.AccessToken, dto.ExpiresAtUtc);

                DateTime exp = dto.ExpiresAtUtc;
                var expUtc = new DateTimeOffset(
                    exp.Kind == DateTimeKind.Utc ? exp : DateTime.SpecifyKind(exp, DateTimeKind.Utc));

                await _state.HydrateAsync(dto.AccessToken, expUtc);
                return true;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "SignIn failed");
                return false;
            }
        }

        public async Task<LoginResponse?> SignInRawAsync(
            string email,
            string password,
            CancellationToken ct = default)
        {
            return await _publicApi.PostJsonAsync<object, LoginResponse>(
                "auth/login",
                new { email, password },
                ct);
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

            var dto = await _publicApi.PostJsonAsync<object, LoginResponse>("auth/register", payload, ct);
            if (!string.IsNullOrWhiteSpace(dto?.AccessToken))
            {
                await _js.InvokeVoidAsync("irsAuth.set", dto!.AccessToken!, dto!.ExpiresAtUtc);

                DateTime exp = dto!.ExpiresAtUtc;
                var expUtc = new DateTimeOffset(
                    exp.Kind == DateTimeKind.Utc ? exp : DateTime.SpecifyKind(exp, DateTimeKind.Utc));

                await _state.HydrateAsync(dto!.AccessToken!, expUtc);
            }
        }

        public Task<WhoAmI?> MeAsync(CancellationToken ct = default)
            => _secureApi.GetJsonAsync<WhoAmI?>("auth/me", ct);

        public Task UpdateMeAsync(string first, string last, CancellationToken ct = default)
            => _secureApi.PatchJsonAsync("auth/me", new { firstName = first, lastName = last }, ct);

        public Task ChangePasswordAsync(string current, string @new, CancellationToken ct = default)
            => _secureApi.PostJsonAsync("auth/me/change-password", new { currentPassword = current, newPassword = @new }, ct);

        public async Task SignOutAsync(CancellationToken ct = default)
        {
            await _state.ClearAsync();
            try { _nav.NavigateTo("/login", forceLoad: true); } catch { /* ignore */ }
        }
    }
}
