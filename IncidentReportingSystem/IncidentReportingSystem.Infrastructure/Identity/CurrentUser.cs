using System.Security.Claims;
using IncidentReportingSystem.Application.Abstractions.Identity;
using Microsoft.AspNetCore.Http;

namespace IncidentReportingSystem.Infrastructure.Identity;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _http;

    public CurrentUser(IHttpContextAccessor http) => _http = http;

    public ClaimsPrincipal? Principal => _http.HttpContext?.User;

    public string? UserId =>
        Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? Principal?.FindFirst("sub")?.Value;

    public string? FirstName =>
        Principal?.FindFirst("given_name")?.Value
        ?? Principal?.FindFirst(ClaimTypes.GivenName)?.Value;

    public string? LastName =>
        Principal?.FindFirst("family_name")?.Value
        ?? Principal?.FindFirst(ClaimTypes.Surname)?.Value;

    public string? Email =>
        Principal?.FindFirst(ClaimTypes.Email)?.Value
        ?? Principal?.FindFirst("email")?.Value;
}
