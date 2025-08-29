using System.Linq;
using System.Security.Claims;

namespace IncidentReportingSystem.API.Common;

public static class ClaimsPrincipalExtensions
{
    public static string GetEmail(this ClaimsPrincipal user)
        => user.FindFirst("email")?.Value
           ?? user.Identity?.Name
           ?? string.Empty;

    public static string[] GetRoles(this ClaimsPrincipal user)
        => user.Claims
               .Where(c => c.Type == ClaimTypes.Role || c.Type == "role" || c.Type == "roles")
               .Select(c => c.Value)
               .Distinct()
               .ToArray();
}
