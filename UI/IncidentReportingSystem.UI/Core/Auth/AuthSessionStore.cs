using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using IncidentReportingSystem.UI.Core.Auth;

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

    public AuthSessionStore(IHttpContextAccessor http, IDataProtectionProvider dp)
    {
        _http = http;
        _protector = dp.CreateProtector("irs.auth.cookie.v1");
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

    public async Task<bool> TryLoadInto(AuthState state)
    {
        var ctx = _http.HttpContext;
        if (ctx is null) return false;

        if (!ctx.Request.Cookies.TryGetValue(CookieName, out var blob) || string.IsNullOrWhiteSpace(blob))
            return false;

        try
        {
            var json = _protector.Unprotect(blob);
            var snap = JsonSerializer.Deserialize<AuthSnapshot>(json);
            if (snap is null || snap.ExpiresAtUtc <= DateTime.UtcNow) return false;

            var me = new AuthModels.WhoAmI(
                snap.UserId ?? "",
                snap.Email ?? "",
                snap.Roles ?? Array.Empty<string>(),
                snap.FirstName,
                snap.LastName,
                snap.DisplayName);

            await state.SetAsync(snap.AccessToken, snap.ExpiresAtUtc);
            
            return true;
        }
        catch
        {
            // Corrupt/old cookie – clear
            Clear();
            return false;
        }
    }

    public void Clear()
    {
        var ctx = _http.HttpContext;
        if (ctx is null) return;
        ctx.Response.Cookies.Delete(CookieName, new CookieOptions { Path = "/" });
    }
}
