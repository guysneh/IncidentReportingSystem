using IncidentReportingSystem.Application.Abstractions.Security;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace IncidentReportingSystem.Infrastructure.Attachments.Services
{
    /// <summary>
    /// HMAC-SHA256 based signed URL service for temporary download links.
    /// Provides fixed query parameter names ("exp" and "sig") and strict validation.
    /// </summary>
    public sealed class SignedUrlService : ISignedUrlService
    {
        private readonly byte[] _key;

        /// <summary>Query parameter name for the expiration timestamp (Unix seconds).</summary>
        public string ExpQueryName => "exp";

        /// <summary>Query parameter name for the URL signature.</summary>
        public string SigQueryName => "sig";

        /// <summary>
        /// Initializes the service with a secret key from configuration.
        /// Falls back to Jwt:Secret if Attachments:DownloadUrlSecret is not provided.
        /// </summary>
        public SignedUrlService(IConfiguration cfg)
        {
            var secret = cfg["Attachments:DownloadUrlSecret"] ?? cfg["Jwt:Secret"]
                         ?? throw new InvalidOperationException("Missing signed URL secret. Configure 'Attachments:DownloadUrlSecret' or 'Jwt:Secret'.");
            _key = Encoding.UTF8.GetBytes(secret);
        }

        /// <summary>
        /// Computes a Base64Url-encoded HMAC-SHA256 signature over "{attachmentId:N}|{expUnixSeconds}".
        /// </summary>
        public string ComputeSignature(Guid attachmentId, long expUnixSeconds)
        {
            using var hmac = new HMACSHA256(_key);
            var payload = $"{attachmentId:N}|{expUnixSeconds}";
            var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return WebEncoders.Base64UrlEncode(bytes);
        }

        /// <summary>
        /// Validates a provided signature by recomputing HMAC and comparing with constant time equality.
        /// </summary>
        public bool IsValid(Guid attachmentId, long expUnixSeconds, string signature)
        {
            byte[] provided;
            try { provided = WebEncoders.Base64UrlDecode(signature); }
            catch { return false; }

            // HMAC-SHA256 output length MUST be 32 bytes
            if (provided.Length != 32)
                return false;

            using var hmac = new HMACSHA256(_key);
            var payload = $"{attachmentId:N}|{expUnixSeconds}";
            var expected = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return CryptographicOperations.FixedTimeEquals(provided, expected);
        }
    }
}
