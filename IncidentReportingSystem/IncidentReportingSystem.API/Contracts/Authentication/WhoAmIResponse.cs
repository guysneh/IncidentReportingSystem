namespace IncidentReportingSystem.API.Contracts.Authentication
{
    public sealed record WhoAmIResponse(string UserId, string Email, string[] Roles);

}
