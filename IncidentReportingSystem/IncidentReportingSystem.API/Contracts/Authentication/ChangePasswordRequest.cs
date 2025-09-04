// API/Contracts/Authentication/ChangePasswordRequest.cs
namespace IncidentReportingSystem.API.Contracts.Authentication;

/// <summary>Request payload for changing the current user's password.</summary>
public sealed class ChangePasswordRequest
{
    public string CurrentPassword { get; init; } = default!;
    public string NewPassword { get; init; } = default!;
}
