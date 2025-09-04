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

        /// <summary>
        /// Generates a signed JWT containing:
        /// - Custom user id claim, optional email, and role claims.
        /// - OIDC-style personal name claims (given_name, family_name) when provided via <paramref name="extraClaims"/>.
        /// - Exactly one Name claim: prefers extraClaims["name"] (display name), otherwise falls back to <paramref name="email"/>.
        /// Expiration is controlled by Jwt:AccessTokenMinutes (or Jwt:ExpirationMinutes), defaulting to 60 minutes.
        /// </summary>
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

            // Email (do NOT set Name yet)
            if (!string.IsNullOrWhiteSpace(email))
            {
                claims.Add(new Claim(ClaimTypesConst.Email, email));
            }

            // Roles
            if (roles != null)
            {
                foreach (var r in roles)
                {
                    if (!string.IsNullOrWhiteSpace(r))
                        claims.Add(new Claim(ClaimTypesConst.Role, r));
                }
            }

            // Helper: safe read from extraClaims
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

            // OIDC-style personal name claims
            var givenName = GetNonEmpty(extraClaims, "given_name");
            var familyName = GetNonEmpty(extraClaims, "family_name");
            if (givenName is not null) claims.Add(new Claim("given_name", givenName));
            if (familyName is not null) claims.Add(new Claim("family_name", familyName));

            // Decide the single Name claim: prefer provided display name; fallback to email
            var effectiveName = GetNonEmpty(extraClaims, "name") ?? email;
            if (!string.IsNullOrWhiteSpace(effectiveName))
                claims.Add(new Claim(ClaimTypesConst.Name, effectiveName));

            // Expiration
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
