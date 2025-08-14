using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace IncidentReportingSystem.IntegrationTests.Utils
{
    public static class AuthenticatedHttpClientFactory
    {
        public static HttpClient CreateClientWithToken(
            WebApplicationFactory<Program> factory,
            Guid? userId = null,
            string username = "test-user",
            string email = "user@example.com",
            IEnumerable<string>? roles = null,
            DateTime? expires = null)
        {
            var client = factory.CreateClient();

            var sp = factory.Services;
            var cfg = sp.GetRequiredService<IConfiguration>();
            var jwtSection = cfg.GetSection("Jwt");
            var issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer missing");
            var audience = jwtSection["Audience"] ?? throw new InvalidOperationException("Jwt:Audience missing");
            var secret = jwtSection["Secret"] ?? throw new InvalidOperationException("Jwt:Secret missing");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var uid = userId ?? Guid.NewGuid();
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, uid.ToString()),
                new(ClaimTypes.NameIdentifier,   uid.ToString()),
                new(ClaimTypes.Name,             username),
                new(ClaimTypes.Email,            email),
            };

            foreach (var role in roles ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(role)) continue;

                // 🔑 Emit both types to satisfy RoleClaimType="role" and any code that reads ClaimTypes.Role
                claims.Add(new Claim("role", role));                 // matches TokenValidationParameters.RoleClaimType
                claims.Add(new Claim(ClaimTypes.Role, role));        // compatibility
            }

            var exp = expires ?? DateTime.UtcNow.AddHours(1);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: exp,
                signingCredentials: creds
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

            return client;
        }
    }
}
