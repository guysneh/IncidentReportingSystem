using IncidentReportingSystem.Application.Abstractions.Security;
using IncidentReportingSystem.Application.Common.Auth;
using IncidentReportingSystem.Infrastructure.Authentication;
using IncidentReportingSystem.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using IncidentReportingSystem.Domain;

namespace IncidentReportingSystem.API.Extensions;

public static class AuthExtensions
{
    public static IServiceCollection AddJwtAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSecret = configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(jwtSecret))
            throw new InvalidOperationException("Missing Jwt:Secret in configuration.");

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasherPBKDF2>();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var jwtSettings = configuration.GetSection("Jwt");

                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(5),
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret missing")) // NOSONAR: same rationale as above; reviewed as safe.
                    ),
                    RoleClaimType = ClaimTypesConst.Role,
                    NameClaimType = ClaimTypesConst.Name
                };
            });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            options.AddPolicy(PolicyNames.CanReadIncidents, p => p.RequireRole(Roles.User, Roles.Admin));
            options.AddPolicy(PolicyNames.CanCreateIncident, p => p.RequireRole(Roles.User, Roles.Admin));
            options.AddPolicy(PolicyNames.CanManageIncidents, p => p.RequireRole(Roles.Admin));
            options.AddPolicy(PolicyNames.CanCommentOnIncident, p => p.RequireRole(Roles.User, Roles.Admin));
            options.AddPolicy(PolicyNames.CanDeleteComment, p => p.RequireRole(Roles.User, Roles.Admin));
        });

        return services;
    }
}
