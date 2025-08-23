namespace IncidentReportingSystem.Application.Common.Auth;

/// <summary>
/// Centralized claim type names used by JWT tokens and validation.
/// Keep in sync with JwtBearerOptions in Program.cs.
/// </summary>
public static class ClaimTypesConst
{
    public const string Role = "role";
    public const string Name = "name";
    public const string Email = "email";
    public const string UserId = "sub";
}

/// <summary>
/// Centralized authorization policy names.
/// </summary>
public static class PolicyNames
{
    public const string CanReadIncidents = nameof(CanReadIncidents);
    public const string CanCreateIncident = nameof(CanCreateIncident);
    public const string CanManageIncidents = nameof(CanManageIncidents);
    public const string CanCommentOnIncident = nameof(CanCommentOnIncident);
    public const string CanDeleteComment = nameof(CanDeleteComment);
}

