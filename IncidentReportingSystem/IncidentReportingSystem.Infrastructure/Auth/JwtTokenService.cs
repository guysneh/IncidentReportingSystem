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

            if (extraClaims != null)
            {
                foreach (var kv in extraClaims)
                {
                    if (kv.Key is ClaimTypesConst.UserId or ClaimTypesConst.Email or ClaimTypesConst.Name or ClaimTypesConst.Role)
                        continue;
                    claims.Add(new Claim(kv.Key, kv.Value));
                }
            }

            var expiresUtc = DateTime.UtcNow.AddMinutes(minutes);

            var jwt = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiresUtc,
                signingCredentials: creds
            );

            var token = new JwtSecurityTokenHandler().WriteToken(jwt);
            return (token, DateTime.SpecifyKind(expiresUtc, DateTimeKind.Utc));
        }
    }
}
