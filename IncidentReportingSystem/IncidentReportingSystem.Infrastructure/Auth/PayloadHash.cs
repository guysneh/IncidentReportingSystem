using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace IncidentReportingSystem.Application.Common.Idempotency
{
    /// <summary>
    /// Provides a stable JSON‑based SHA‑256 hash for request payload identity.
    /// </summary>
    public static class PayloadHash
    {
        /// <summary>
        /// Serializes <paramref name="payload"/> to JSON and computes a SHA‑256 hex digest.
        /// </summary>
        public static string ComputeStableHash<T>(T payload)
        {
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = false });
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(json));
            return Convert.ToHexString(bytes);
        }
    }
}
