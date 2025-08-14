using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IncidentReportingSystem.Domain.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace IncidentReportingSystem.API.Auth
{
    public static class JwtTokenGenerator
    {
        public static string GenerateToken(
            IOptions<JwtSettings> options,
            string userId,
            IEnumerable<string> roles,
            string? email = null,
            IDictionary<string, string>? extraClaims = null)
        {
            var jwt = options.Value;

            var claims = new List<Claim>
            {
                // Match TokenValidationParameters.NameClaimType ("sub")
                new(ClaimTypesConst.Name, userId),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (!string.IsNullOrWhiteSpace(email))
            {
                // Optional email claim
                claims.Add(new Claim(ClaimTypesConst.Email, email));
            }

            // Match TokenValidationParameters.RoleClaimType ("role")
            foreach (var role in roles.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                claims.Add(new Claim(ClaimTypesConst.Role, role));
            }

            if (extraClaims != null)
            {
                foreach (var kv in extraClaims)
                {
                    claims.Add(new Claim(kv.Key, kv.Value));
                }
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Try to read minutes from common property names; fallback to 60
            var minutes = 60;
            var accessTokenMinutesProp = typeof(JwtSettings).GetProperty("AccessTokenMinutes");
            var expirationMinutesProp = typeof(JwtSettings).GetProperty("ExpirationMinutes");
            if (accessTokenMinutesProp?.GetValue(jwt) is int m1 && m1 > 0) minutes = m1;
            else if (expirationMinutesProp?.GetValue(jwt) is int m2 && m2 > 0) minutes = m2;

            var token = new JwtSecurityToken(
                issuer: jwt.Issuer,
                audience: jwt.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(minutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
