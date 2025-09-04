using System;

namespace IncidentReportingSystem.Application.Abstractions.Security
{
    /// <summary>
    /// Generates and validates HMAC-based signatures for temporary, anonymous download URLs.
    /// Keeps cryptographic concerns outside Web and Controllers.
    /// </summary>
    public interface ISignedUrlService
    {
        /// <summary>Name of the query parameter that carries the Unix expiry timestamp.</summary>
        string ExpQueryName { get; }

        /// <summary>Name of the query parameter that carries the signature value.</summary>
        string SigQueryName { get; }

        /// <summary>
        /// Computes a Base64Url-encoded HMAC-SHA256 signature over "{attachmentId:N}|{expiresUnixSeconds}".
        /// </summary>
        string ComputeSignature(Guid attachmentId, long expiresUnixSeconds);

        /// <summary>
        /// Validates the provided signature against the expected HMAC-SHA256 value.
        /// Implements constant-time comparison to mitigate timing attacks.
        /// </summary>
        bool IsValid(Guid attachmentId, long expiresUnixSeconds, string providedSignature);
    }
}
