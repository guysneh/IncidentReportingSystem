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

    /// <summary>PBKDF2 password hash (base64). Will be set in a later step.</summary>
    public string PasswordHash { get; set; } = null!;

    /// <summary>PBKDF2 salt (base64). Will be set in a later step.</summary>
    public string PasswordSalt { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
