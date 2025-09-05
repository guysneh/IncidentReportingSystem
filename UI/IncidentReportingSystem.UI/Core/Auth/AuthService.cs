using IncidentReportingSystem.UI.Core.Auth;
using IncidentReportingSystem.UI.Core.Http;
using Microsoft.JSInterop;

public sealed class AuthService : IAuthService
{
    private readonly PublicApiClient _publicApi;
    private readonly SecureApiClient _secureApi;
    private readonly IJSRuntime _js;
    private readonly AuthState _state;
    private readonly ILogger<AuthService> _log;

    public AuthService(
        PublicApiClient publicApi,
        SecureApiClient secureApi,
        IJSRuntime js,
        AuthState state,
        ILogger<AuthService> log)
    {
        _publicApi = publicApi;
        _secureApi = secureApi;
        _js = js;
        _state = state;
        _log = log;
    }

    private sealed class LoginResponse
    {
        public string? AccessToken { get; set; }
        public DateTimeOffset? ExpiresAtUtc { get; set; }
    }

    public async Task<bool> SignInAsync(string email, string password, CancellationToken ct = default)
    {
        try
        {
            var dto = await _publicApi.PostJsonAsync<object, LoginResponse>(
                "auth/login", new { email, password }, ct);

            if (dto?.AccessToken is null) return false;

            await _js.InvokeVoidAsync("irsAuth.set", dto.AccessToken, dto.ExpiresAtUtc);
            await _state.SetAsync(dto.AccessToken);
            return true;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "SignIn failed");
            return false;
        }
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
            await _state.SetAsync(dto!.AccessToken!);
        }
    }

    public Task<AuthModels.WhoAmI?> MeAsync(CancellationToken ct = default)
        => _secureApi.GetJsonAsync<AuthModels.WhoAmI?>("auth/me", ct);

    public Task UpdateMeAsync(string first, string last, CancellationToken ct = default)
        => _secureApi.PatchJsonAsync("auth/me", new { firstName = first, lastName = last }, ct);

    public Task ChangePasswordAsync(string current, string @new, CancellationToken ct = default)
        => _secureApi.PostJsonAsync("auth/me/change-password", new { currentPassword = current, newPassword = @new }, ct);

    public async Task SignOutAsync(CancellationToken ct = default)
    {
        try { await _js.InvokeVoidAsync("irsAuth.clear"); } catch { }
        await _state.ClearAsync();
    }
}
