using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace IncidentReportingSystem.API.Auth;

/// <summary>
/// Static utility class for generating JWT tokens for demo purposes.
/// </summary>
public static class JwtTokenGenerator
{
    public static string GenerateToken(IConfiguration configuration, string userId, string role)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];
        var secret = jwtSection["Secret"];
        var expiryMinutes = int.Parse(jwtSection["ExpiryMinutes"]);

        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException("JWT Secret, Issuer or Audience is missing");
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role),
            new Claim("scope", "incident:read incident:write") 
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
