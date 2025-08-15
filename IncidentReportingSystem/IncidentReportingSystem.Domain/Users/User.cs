using System.Diagnostics.CodeAnalysis;

namespace IncidentReportingSystem.Domain.Users
{
    /// <summary>
    /// Application user persisted in the database.
    /// Supports multiple roles via a PostgreSQL text[] column.
    /// </summary>
    public class User
    {
        public Guid Id { get; set; }

        /// <summary>Email/username used for login.</summary>
        public string Email { get; set; } = null!;

        /// <summary>Upper-cased email for case-insensitive lookups.</summary>
        public string NormalizedEmail { get; set; } = null!;

        /// <summary>One or more roles (e.g. Roles.Admin, Roles.User).</summary>
        [SuppressMessage("Performance", "CA1819:Properties should not return arrays",
            Justification = "EF Core maps PostgreSQL text[]; array property is intentional.")]
        public string[] Roles { get; private set; } = Array.Empty<string>();

        /// <summary>PBKDF2 password hash (derived key). Stored as bytea in PostgreSQL.</summary>
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();

        /// <summary>Random salt used for password hashing. Stored as bytea in PostgreSQL.</summary>
        public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>Validates and assigns roles using the centralized Roles.Allowed set (case-insensitive).</summary>
        public void SetRoles(IEnumerable<string> roles)
        {
            if (roles is null) throw new ArgumentNullException(nameof(roles));

            var distinct = roles
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (distinct.Length == 0)
                throw new ArgumentException("At least one role is required.", nameof(roles));

            if (distinct.Any(r => !Auth.Roles.Allowed.Contains(r)))
                throw new ArgumentException("One or more roles are invalid.", nameof(roles));

            Roles = distinct;
        }
    }
}
