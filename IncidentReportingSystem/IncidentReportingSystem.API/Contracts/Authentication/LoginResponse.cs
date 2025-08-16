namespace IncidentReportingSystem.API.Contracts.Authentication
{
    public sealed class LoginResponse
    {
        public string AccessToken { get; init; } = string.Empty;
        public DateTimeOffset ExpiresAtUtc { get; init; }
    }
}