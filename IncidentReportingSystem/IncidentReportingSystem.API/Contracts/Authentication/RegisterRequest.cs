namespace IncidentReportingSystem.API.Contracts.Authentication;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string? Role = null,      // legacy (single)
    string[]? Roles = null,   // new (multiple)
    string? FirstName = null,
    string? LastName = null
);
