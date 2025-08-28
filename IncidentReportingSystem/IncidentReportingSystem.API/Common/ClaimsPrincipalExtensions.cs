using System.Linq;
using System.Security.Claims;

namespace IncidentReportingSystem.API.Common;

internal static class ClaimsPrincipalExtensions
{
    internal static string GetEmail(this ClaimsPrincipal user)
        => user.FindFirst("email")?.Value
           ?? user.Identity?.Name
           ?? string.Empty;

    internal static string[] GetRoles(this ClaimsPrincipal user)
        => user.Claims
               .Where(c => c.Type == ClaimTypes.Role || c.Type == "role" || c.Type == "roles")
               .Select(c => c.Value)
               .Distinct()
               .ToArray();
}
