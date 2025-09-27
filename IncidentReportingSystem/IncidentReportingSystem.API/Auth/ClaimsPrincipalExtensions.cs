using System.Security.Claims;

namespace IncidentReportingSystem.API.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Returns the caller's user id (GUID) from common claim types.
    /// Throws if the claim is missing or not a valid GUID.
    /// </summary>
    public static Guid RequireUserId(this ClaimsPrincipal user)
    {
        var s = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (Guid.TryParse(s, out var id)) return id;
        throw new UnauthorizedAccessException("Missing or invalid user id claim.");
    }

    /// <summary>
    /// Returns true if the principal has the Admin role (covers typical role claim mappings).
    /// </summary>
    public static bool IsAdminRole(this ClaimsPrincipal user)
    {
        return user.IsInRole("Admin")
               || user.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin")
               || user.Claims.Any(c => c.Type == "role" && c.Value == "Admin");
    }
}
