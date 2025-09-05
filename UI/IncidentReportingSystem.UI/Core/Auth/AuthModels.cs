using System.Text.Json.Serialization;

namespace IncidentReportingSystem.UI.Core.Auth;

public static class AuthModels
{
    // Requests
    public sealed record LoginRequest(string Email, string Password);
    public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
    public sealed record UpdateMeRequest(string FirstName, string LastName);
    public sealed record RegisterRequest(
        string Email,
        string Password,
        string? Role,
        string[]? Roles,
        string FirstName,
        string LastName
    );

    // Responses
    public sealed record LoginResponse(
         [property: JsonPropertyName("accessToken")] string AccessToken,
         [property: JsonPropertyName("expiresAtUtc")] DateTime ExpiresAtUtc);

    public sealed record RegisterCreatedResponse(
        [property: JsonPropertyName("id")] string id,
        [property: JsonPropertyName("email")] string email,
        [property: JsonPropertyName("accessToken")] string accessToken,
        [property: JsonPropertyName("expiresAtUtc")] DateTime expiresAtUtc,
        [property: JsonPropertyName("displayName")] string? displayName);
    public sealed record UserProfileDto(Guid Id, string Email, string? FirstName, string? LastName, string? DisplayName);
    public sealed record WhoAmI(string UserId, string Email, string[] Roles, string? FirstName, string? LastName, string? DisplayName);
}
