using System;

namespace IncidentReportingSystem.Domain.Users;

/// <summary>
/// Application user persisted in the database.
/// Supports multiple roles via a PostgreSQL text[] column.
/// Password fields will be added/used by the auth service in a later step.
/// </summary>
public class User
{
    public Guid Id { get; set; }

    /// <summary>Email/username used for login.</summary>
    public string Email { get; set; } = null!;

    /// <summary>Upper-cased email for case-insensitive lookups.</summary>
    public string NormalizedEmail { get; set; } = null!;

    /// <summary>One or more roles (e.g. "Admin", "User").</summary>
    public string[] Roles { get; set; } = Array.Empty<string>();

    /// <summary>
    /// PBKDF2 password hash (derived key). Stored as bytea in PostgreSQL.
    /// </summary>
    public byte[] PasswordHash { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Random salt used for password hashing. Stored as bytea in PostgreSQL.
    /// </summary>
    public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
