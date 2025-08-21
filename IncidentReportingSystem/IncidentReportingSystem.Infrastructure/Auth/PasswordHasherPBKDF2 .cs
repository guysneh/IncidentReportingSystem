using System.Security.Cryptography;
using IncidentReportingSystem.Application.Abstractions.Security;
using IncidentReportingSystem.Infrastructure.Auth;
using Microsoft.Extensions.Options;

namespace IncidentReportingSystem.Infrastructure.Authentication
{
    /// <summary>
    /// PBKDF2 password hasher using HMAC-SHA256.
    /// </summary>
    public sealed class PasswordHasherPBKDF2 : IPasswordHasher
    {
        private readonly PasswordHashingOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="PasswordHasherPBKDF2"/> class.
        /// </summary>
        public PasswordHasherPBKDF2(IOptions<PasswordHashingOptions> options)
        {
            _options = options.Value;
        }

        /// <inheritdoc />
        public (byte[] Hash, byte[] Salt) HashPassword(string password, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            // Generate a cryptographically strong random salt.
            var salt = RandomNumberGenerator.GetBytes(_options.SaltSizeBytes);

            // Derive a key using PBKDF2 with HMAC-SHA256.
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, _options.Iterations, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(_options.KeySizeBytes);

            return (hash, salt);
        }

        /// <inheritdoc />
        public bool Verify(string password, byte[] hash, byte[] salt, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            // Recompute the derived key using the provided salt and compare in constant time.
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, _options.Iterations, HashAlgorithmName.SHA256);
            var computed = pbkdf2.GetBytes(_options.KeySizeBytes);

            return CryptographicOperations.FixedTimeEquals(computed, hash);
        }
    }
}
