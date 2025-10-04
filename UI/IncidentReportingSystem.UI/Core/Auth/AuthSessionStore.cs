using IncidentReportingSystem.UI.Core.Auth;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.JSInterop;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace IncidentReportingSystem.UI.Core.Auth;

public sealed record AuthSnapshot(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    string? UserId,
    string? Email,
    string? FirstName,
    string? LastName,
    string? DisplayName,
    string[] Roles);

public sealed class AuthSessionStore
{
    private const string CookieName = ".irs.auth";
    private readonly IHttpContextAccessor _http;
    private readonly IDataProtector _protector;
    private readonly IJSRuntime _js;
    private readonly AuthState _state;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _loaded;
    private sealed class AuthBlob { public string? t { get; set; } public long exp { get; set; } }


    public bool IsLoaded => _loaded;

    public AuthSessionStore(IHttpContextAccessor http, IDataProtectionProvider dp, IJSRuntime js, AuthState state)
    {
        _http = http;
        _protector = dp.CreateProtector("irs.auth.cookie.v1");
        _js = js;
        _state = state;
    }

    public void Save(AuthSnapshot snap)
    {
        var json = JsonSerializer.Serialize(snap);
        var blob = _protector.Protect(json);

        var ctx = _http.HttpContext ?? throw new InvalidOperationException("No HttpContext.");
        ctx.Response.Cookies.Append(
            CookieName,
            blob,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = ctx.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = snap.ExpiresAtUtc > DateTime.UtcNow ? snap.ExpiresAtUtc : DateTime.UtcNow.AddDays(1)
            });
    }

    public async Task LoadAsync()
    {
        if (_loaded) return;
        await _gate.WaitAsync();
        try
        {
            if (_loaded) return;

            var raw = await _js.InvokeAsync<string?>("irsAuth.getRaw");
            if (!string.IsNullOrWhiteSpace(raw))
            {
                AuthBlob? blob = null;
                try { blob = System.Text.Json.JsonSerializer.Deserialize<AuthBlob>(raw); } catch { /* ignore */ }

                if (!string.IsNullOrWhiteSpace(blob?.t) && blob!.exp > 0)
                {
                    var exp = DateTimeOffset.FromUnixTimeMilliseconds(blob.exp);
                    await _state.HydrateAsync(blob.t!, exp);
                }
            }

            _loaded = true;
        }
        finally { _gate.Release(); }
    }

    public void Clear()
    {
        var ctx = _http.HttpContext;
        if (ctx is null) return;
        ctx.Response.Cookies.Delete(CookieName, new CookieOptions { Path = "/" });
    }
}
