using System.Text.Json;

namespace IncidentReportingSystem.IntegrationTests.Utils
{
    /// <summary>
    /// Minimal JWT helpers for tests: read payload, extract userId and roles.
    /// </summary>
    public static class JwtTestHelpers
    {
        public static Guid? ExtractUserId(HttpClient client)
        {
            var auth = client.DefaultRequestHeaders.Authorization;
            if (auth is null || !string.Equals(auth.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(auth.Parameter))
                return null;
            return ExtractUserIdFromToken(auth.Parameter);
        }

        public static Guid? ExtractUserIdFromToken(string token)
        {
            try
            {
                var payload = ReadPayload(token);
                var keys = new[]
                {
                    "sub",
                    "nameidentifier",
                    "nameid",
                    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
                    "userId"
                };
                foreach (var k in keys)
                {
                    if (payload.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String && Guid.TryParse(v.GetString(), out var g))
                        return g;
                }
            }
            catch { }
            return null;
        }

        public static JsonElement ReadPayload(string token)
        {
            var parts = token.Split('.');
            if (parts.Length < 2) throw new ArgumentException("Invalid JWT", nameof(token));
            var jsonBytes = Base64UrlDecode(parts[1]);
            using var doc = JsonDocument.Parse(jsonBytes);
            return doc.RootElement.Clone();
        }

        public static HashSet<string> ExtractRoles(string token)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var p = ReadPayload(token);
                AddRolesFromProp(p, "role", set);
                AddRolesFromProp(p, "roles", set);
                AddRolesFromProp(p, "http://schemas.microsoft.com/ws/2008/06/identity/claims/role", set);
            }
            catch { }
            return set;
        }

        private static void AddRolesFromProp(JsonElement p, string propName, HashSet<string> set)
        {
            if (!p.TryGetProperty(propName, out var v)) return;

            if (v.ValueKind == JsonValueKind.Array)
            {
                foreach (var e in v.EnumerateArray())
                    if (e.ValueKind == JsonValueKind.String) set.Add(e.GetString()!);
            }
            else if (v.ValueKind == JsonValueKind.String)
            {
                var s = v.GetString()!;
                if (s.Contains(','))
                {
                    foreach (var part in s.Split(',', StringSplitOptions.RemoveEmptyEntries))
                        set.Add(part.Trim());
                }
                else set.Add(s);
            }
        }

        private static byte[] Base64UrlDecode(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }
            return Convert.FromBase64String(s);
        }
    }
}
