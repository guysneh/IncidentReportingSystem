using System.Security.Claims;

namespace IncidentReportingSystem.API.Authentication;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Returns the caller's user id (GUID) from common claim types.
    /// Throws if the claim is missing or not a valid GUID.
    /// </summary>
    public static Guid RequireUserId(this ClaimsPrincipal user)
    {
        var value =
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            user.FindFirst("sub")?.Value ??            // JWT subject
            user.FindFirst("userId")?.Value ??
            user.FindFirst("uid")?.Value;

        if (Guid.TryParse(value, out var id))
            return id;

        throw new InvalidOperationException("User id claim is missing or invalid.");
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
