namespace IncidentReportingSystem.Infrastructure.Auth;

/// <summary>
/// Tunable options for password hashing (PBKDF2).
/// </summary>
public sealed class PasswordHashingOptions
{
    /// <summary>
    /// Number of PBKDF2 iterations. Higher values increase computational cost for attackers.
    /// </summary>
    public int Iterations { get; init; } = 200_000;

    /// <summary>
    /// Size, in bytes, of the per-user random salt (e.g., 16 for 128-bit).
    /// </summary>
    public int SaltSizeBytes { get; init; } = 16;

    /// <summary>
    /// Size, in bytes, of the derived key (e.g., 32 for 256-bit).
    /// </summary>
    public int KeySizeBytes { get; init; } = 32;
}
