namespace IncidentReportingSystem.API.Contracts.Authentication
{
    /// <summary>
    /// Describes the authenticated user as exposed by the API.
    /// Extended to include optional profile fields for richer UX.
    /// This change is backward-compatible: existing consumers can ignore new fields.
    /// </summary>
    /// <param name="UserId">User identifier (GUID string).</param>
    /// <param name="Email">Primary email address.</param>
    /// <param name="Roles">Distinct set of roles.</param>
    /// <param name="FirstName">Optional given name (may be null).</param>
    /// <param name="LastName">Optional family name (may be null).</param>
    /// <param name="DisplayName">Optional display name; falls back to email if not present.</param>
    public sealed record WhoAmIResponse(
        string UserId,
        string Email,
        string[] Roles,
        string? FirstName,
        string? LastName,
        string? DisplayName
    );
}
