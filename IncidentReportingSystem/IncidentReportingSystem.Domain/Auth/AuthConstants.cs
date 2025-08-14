namespace IncidentReportingSystem.Domain.Auth
{
    /// <summary>
    /// Centralized claim type names used by JWT tokens and validation.
    /// Keep in sync with JwtBearerOptions in Program.cs.
    /// </summary>
    public static class ClaimTypesConst
    {
        public const string Role = "role"; // TokenValidationParameters.RoleClaimType
        public const string Name = "sub";  // TokenValidationParameters.NameClaimType
    }

    /// <summary>
    /// Centralized authorization policy names.
    /// </summary>
    public static class PolicyNames
    {
        public const string CanReadIncidents = nameof(CanReadIncidents);
        public const string CanCreateIncident = nameof(CanCreateIncident);
        public const string CanManageIncidents = nameof(CanManageIncidents);
    }
}
