using System.Text.Json;
using Microsoft.JSInterop;

namespace IncidentReportingSystem.UI.Core.Auth
{
    // ה-blob החדש שנשמר ב-localStorage על-ידי auth.js
    file sealed class AuthBlob
    {
        public string? t { get; set; } // access token
        public long exp { get; set; }  // expiry in ms since epoch (UTC)
    }

    public sealed class AuthState
    {
        private bool _isHydrated;
        public bool IsHydrated => _isHydrated;

        public string? AccessToken { get; private set; }
        public DateTimeOffset? ExpiresAtUtc { get; private set; }

        public bool IsAuthorized =>
            !string.IsNullOrWhiteSpace(AccessToken) &&
            ExpiresAtUtc is DateTimeOffset exp &&
            exp > DateTimeOffset.UtcNow;

        // event אסינכרוני לשינויים (תואם לשימושים שלך ב-Welcome/AuthGuard/Me)
        public event Func<Task>? Changed;

        public bool HasRole(string role) 
        {
            return true;
        }
        public async Task HydrateAsync(string token, DateTimeOffset expiresAtUtc)
        {
            AccessToken = token;
            ExpiresAtUtc = expiresAtUtc;
            _isHydrated = true;
            if (Changed is not null) await Changed.Invoke();
        }

        public async Task EnsureHydratedAsync(IJSRuntime js)
        {
            if (_isHydrated) return;

            string? raw = null;
            try { raw = await js.InvokeAsync<string?>("irsAuth.getRaw"); }
            catch { /* אם auth.js לא נטען עדיין – לא להפיל */ }

            if (!string.IsNullOrWhiteSpace(raw))
            {
                try
                {
                    var blob = JsonSerializer.Deserialize<AuthBlob>(raw);
                    if (!string.IsNullOrWhiteSpace(blob?.t) && blob!.exp > 0)
                    {
                        var exp = DateTimeOffset.FromUnixTimeMilliseconds(blob.exp);
                        await HydrateAsync(blob.t!, exp);
                        return;
                    }
                }
                catch
                {
                    // JSON פגום? נמשיך ל"חסר טוקן"
                }
            }

            // אין טוקן – מסמנים hydrated כדי שה־UI לא יישאר תקוע
            _isHydrated = true;
            if (Changed is not null) await Changed.Invoke();
        }

        public async Task ClearAsync(IJSRuntime js)
        {
            try { await js.InvokeVoidAsync("irsAuth.clear"); } catch { /* ignore */ }
            AccessToken = null;
            ExpiresAtUtc = null;
            _isHydrated = true;
            if (Changed is not null) await Changed.Invoke();
        }
    }
}
