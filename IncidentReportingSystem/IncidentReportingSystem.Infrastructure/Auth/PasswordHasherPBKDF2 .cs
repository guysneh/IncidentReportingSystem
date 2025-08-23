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
            if (options is null) throw new ArgumentNullException(nameof(options));
            var value = options.Value ?? throw new ArgumentNullException(nameof(options.Value));

            if (value.SaltSizeBytes <= 0)
                throw new ArgumentOutOfRangeException(nameof(value.SaltSizeBytes), "SaltSizeBytes must be > 0.");
            if (value.KeySizeBytes <= 0)
                throw new ArgumentOutOfRangeException(nameof(value.KeySizeBytes), "KeySizeBytes must be > 0.");
            if (value.Iterations <= 0)
                throw new ArgumentOutOfRangeException(nameof(value.Iterations), "Iterations must be > 0.");

            _options = value;
        }

        /// <inheritdoc />
        public (byte[] Hash, byte[] Salt) HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentNullException(nameof(password));

            // Generate a cryptographically strong random salt.
            var salt = RandomNumberGenerator.GetBytes(_options.SaltSizeBytes);

            // Derive a key using PBKDF2 with HMAC-SHA256.
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, _options.Iterations, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(_options.KeySizeBytes);

            return (hash, salt);
        }

        /// <inheritdoc />
        public bool Verify(string password, byte[] hash, byte[] salt)
        {
            if (string.IsNullOrEmpty(password)) return false;
            if (hash is null || salt is null) return false;
            if (hash.Length != _options.KeySizeBytes) return false;
            if (salt.Length != _options.SaltSizeBytes) return false;

            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                _options.Iterations,
                HashAlgorithmName.SHA256);

            var computed = pbkdf2.GetBytes(_options.KeySizeBytes);
            return CryptographicOperations.FixedTimeEquals(computed, hash);
        }

    }
}
