using IncidentReportingSystem.Application.Abstractions.Security;
using IncidentReportingSystem.Application.Common.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text; 
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace IncidentReportingSystem.Infrastructure.Auth
{
    public sealed class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _config;

        public JwtTokenService(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public (string token, DateTimeOffset expiresAtUtc) Generate(
     string userId,
     IEnumerable<string> roles,
     string? email = null,
     IDictionary<string, string>? extraClaims = null)
        {
            var issuer = _config["Jwt:Issuer"] ?? string.Empty;
            var audience = _config["Jwt:Audience"] ?? string.Empty;
            var secret = _config["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

            var minutesStr = _config["Jwt:AccessTokenMinutes"] ?? _config["Jwt:ExpirationMinutes"];
            var minutes = int.TryParse(minutesStr, out var m) && m > 0 ? m : 60;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim> { new(ClaimTypesConst.UserId, userId) };

            if (!string.IsNullOrWhiteSpace(email))
            {
                claims.Add(new Claim(ClaimTypesConst.Email, email));
                claims.Add(new Claim(ClaimTypesConst.Name, email));
            }

            if (roles != null)
            {
                foreach (var r in roles)
                    if (!string.IsNullOrWhiteSpace(r))
                        claims.Add(new Claim(ClaimTypesConst.Role, r));
            }

            // Local helper to safely get a non-empty claim
            static string? GetNonEmpty(IDictionary<string, string>? dict, string key)
            {
                if (dict is null) return null;
                return dict.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                    ? value
                    : null;
            }

            // Merge arbitrary extra claims (excluding reserved ones)
            if (extraClaims is not null)
            {
                foreach (var kv in extraClaims)
                {
                    if (kv.Key is ClaimTypesConst.UserId or ClaimTypesConst.Email or ClaimTypesConst.Name or ClaimTypesConst.Role)
                        continue;

                    if (!string.IsNullOrWhiteSpace(kv.Value))
                        claims.Add(new Claim(kv.Key, kv.Value));
                }
            }

            // OIDC-style standard name claims
            var givenName = GetNonEmpty(extraClaims, "given_name");
            if (givenName is not null)
                claims.Add(new Claim("given_name", givenName));

            var familyName = GetNonEmpty(extraClaims, "family_name");
            if (familyName is not null)
                claims.Add(new Claim("family_name", familyName));

            var displayName = GetNonEmpty(extraClaims, "name");
            if (displayName is not null)
                claims.Add(new Claim("name", displayName));

            // ✅ here we define expiresAtUtc correctly
            var expiresAtUtc = DateTime.UtcNow.AddMinutes(minutes);

            var jwt = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiresAtUtc,
                signingCredentials: creds
            );

            var token = new JwtSecurityTokenHandler().WriteToken(jwt);
            return (token, DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc));
        }

    }
}
