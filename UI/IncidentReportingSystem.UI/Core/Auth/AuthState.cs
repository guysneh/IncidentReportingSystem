using System.Text;
using System.Text.Json;
using Microsoft.JSInterop;
using System.Collections.Generic;

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

        // קאש של תפקידי המשתמש (מתעדכן עם שינוי טוקן)
        private HashSet<string>? _rolesCache;

        public bool HasRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role)) return false;
            if (!IsAuthorized || string.IsNullOrWhiteSpace(AccessToken)) return false;

            // בונים את הקאש בפעם הראשונה
            _rolesCache ??= ParseRolesFromJwt(AccessToken);

            return _rolesCache.Contains(role);
        }

        private static HashSet<string> ParseRolesFromJwt(string jwt)
        {
            var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var parts = jwt.Split('.');
                if (parts.Length < 2) return roles;

                var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
                using var doc = JsonDocument.Parse(payloadJson);
                var root = doc.RootElement;

                // 1) "role" (string) או "roles" (array/string)
                AddClaimValues(root, "role", roles);
                AddClaimValues(root, "roles", roles);

                // 2) claim התקני של ASP.NET (ישן)
                AddClaimValues(root, "http://schemas.microsoft.com/ws/2008/06/identity/claims/role", roles);

                // 3) Keycloak style: realm_access.roles = [ ... ]
                if (root.TryGetProperty("realm_access", out var realmAccess) &&
                    realmAccess.ValueKind == JsonValueKind.Object &&
                    realmAccess.TryGetProperty("roles", out var realmRoles) &&
                    realmRoles.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in realmRoles.EnumerateArray())
                        if (el.ValueKind == JsonValueKind.String && el.GetString() is string r && !string.IsNullOrWhiteSpace(r))
                            roles.Add(r);
                }
            }
            catch
            {
                // אם הטוקן לא תקין/לא מפוענח – פשוט נחזיר סט ריק
            }

            return roles;
        }

        private static void AddClaimValues(JsonElement root, string claimName, HashSet<string> sink)
        {
            if (!root.TryGetProperty(claimName, out var el)) return;

            if (el.ValueKind == JsonValueKind.String)
            {
                var v = el.GetString();
                if (!string.IsNullOrWhiteSpace(v)) sink.Add(v);
            }
            else if (el.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in el.EnumerateArray())
                    if (item.ValueKind == JsonValueKind.String && item.GetString() is string v && !string.IsNullOrWhiteSpace(v))
                        sink.Add(v);
            }
        }

        private static byte[] Base64UrlDecode(string input)
        {
            // תקן Base64Url → Base64
            input = input.Replace('-', '+').Replace('_', '/');
            switch (input.Length % 4)
            {
                case 2: input += "=="; break;
                case 3: input += "="; break;
            }
            return Convert.FromBase64String(input);
        }

        public async Task HydrateAsync(string token, DateTimeOffset expiresAtUtc)
        {
            AccessToken = token;
            ExpiresAtUtc = expiresAtUtc;
            _rolesCache = null; // לאפס קאש תפקידים
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
            _rolesCache = null; // לאפס קאש תפקידים
            _isHydrated = true;
            if (Changed is not null) await Changed.Invoke();
        }
    }
}
