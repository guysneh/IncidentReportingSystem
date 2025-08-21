namespace IncidentReportingSystem.Domain;

/// <summary>
/// Centralized application roles. Use these constants rather than magic strings.
/// </summary>
public static class Roles
{
    public const string Admin = nameof(Admin);
    public const string User = nameof(User);

    /// <summary>All allowed role names (case-insensitive).</summary>
    public static readonly ISet<string> Allowed =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Admin, User
        };
}
