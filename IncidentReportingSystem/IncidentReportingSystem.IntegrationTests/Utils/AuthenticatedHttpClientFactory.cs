using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Common.Auth;
using IncidentReportingSystem.Domain;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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

            // ---------- 1) ensure user exists in the SAME DB used by the app ----------
            using (var scope = factory.Services.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var normalized = email.Trim().ToUpperInvariant();
                Guid effectiveUserId;

                if (userId is { } providedId)
                {
                    // If caller forced a specific Id, try to use it.
                    var byId = repo.GetByIdAsync(providedId, default).GetAwaiter().GetResult();
                    if (byId is null)
                    {
                        // Avoid unique-email conflicts: if email is already taken by someone else, synthesize a unique email.
                        var existingByEmail = repo.FindByNormalizedEmailAsync(normalized, default).GetAwaiter().GetResult();
                        var finalEmail = existingByEmail is null ? email : $"{providedId:N}@itest.local";

                        var u = new User
                        {
                            Id = providedId,
                            Email = finalEmail,
                            NormalizedEmail = finalEmail.Trim().ToUpperInvariant(),
                            CreatedAtUtc = DateTime.UtcNow,
                            PasswordHash = new byte[32],
                            PasswordSalt = new byte[16]
                        };
                        u.SetRoles(new[] { Roles.User });
                        repo.AddAsync(u, default).GetAwaiter().GetResult();
                        uow.SaveChangesAsync(default).GetAwaiter().GetResult();
                    }
                    effectiveUserId = providedId;
                }
                else
                {
                    // No Id provided: if email exists — adopt its Id; else create a new user with this email.
                    var existing = repo.FindByNormalizedEmailAsync(normalized, default).GetAwaiter().GetResult();
                    if (existing is null)
                    {
                        var newId = Guid.NewGuid();
                        var u = new User
                        {
                            Id = newId,
                            Email = email,
                            NormalizedEmail = normalized,
                            CreatedAtUtc = DateTime.UtcNow,
                            PasswordHash = new byte[32],
                            PasswordSalt = new byte[16]
                        };
                        u.SetRoles(new[] { Roles.User });
                        repo.AddAsync(u, default).GetAwaiter().GetResult();
                        uow.SaveChangesAsync(default).GetAwaiter().GetResult();
                        effectiveUserId = newId;
                    }
                    else
                    {
                        effectiveUserId = existing.Id;
                    }
                }

                // ---------- 2) issue JWT for the *existing* user ----------
                var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                var jwtSection = cfg.GetSection("Jwt");
                var issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer missing");
                var audience = jwtSection["Audience"] ?? throw new InvalidOperationException("Jwt:Audience missing");
                var secret = jwtSection["Secret"] ?? throw new InvalidOperationException("Jwt:Secret missing");

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var claims = new List<Claim>
                {
                    new(JwtRegisteredClaimNames.Sub, effectiveUserId.ToString()),
                    new(ClaimTypes.NameIdentifier,   effectiveUserId.ToString()),
                    new(ClaimTypes.Name,             username),
                    new(ClaimTypes.Email,            email)
                };

                foreach (var role in roles ?? Array.Empty<string>())
                {
                    if (string.IsNullOrWhiteSpace(role)) continue;
                    claims.Add(new Claim(ClaimTypesConst.Role, role)); // "role"
                    claims.Add(new Claim(ClaimTypes.Role, role));      // fallback
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
            }

            return client;
        }
    }
}
