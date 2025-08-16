namespace IncidentReportingSystem.Application.Authentication
{
    /// <summary>
    /// Application port for issuing JWT access tokens.
    /// </summary>
    public interface IJwtTokenService
    {
        /// <summary>
        /// Generates a JWT access token and returns the token string and its absolute expiry (UTC).
        /// </summary>
        (string token, DateTimeOffset expiresAtUtc) Generate(
            string userId,
            IEnumerable<string> roles,
            string? email = null,
            IDictionary<string, string>? extraClaims = null);
    }
}
