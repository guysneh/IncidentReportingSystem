using IncidentReportingSystem.Application.Abstractions.Security;
using IncidentReportingSystem.Application.Common.Auth;
using Microsoft.AspNetCore.Http;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;

namespace IncidentReportingSystem.Infrastructure.Auth
{
    /// <summary>
    /// Extracts the current user's identity from <see cref="IHttpContextAccessor"/>.
    /// Looks for standard claim types: <see cref="ClaimTypes.NameIdentifier"/> and <see cref="JwtRegisteredClaimNames.Sub"/>.
    /// </summary>
    public sealed class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc />
        public Guid UserIdOrThrow()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user is null || !user.Identity?.IsAuthenticated == true)
                throw new InvalidOperationException(AuthErrors.MissingUserId);

            // Prefer NameIdentifier; fall back to JWT "sub"
            var raw = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (!Guid.TryParse(raw, out var id))
                throw new InvalidOperationException(AuthErrors.MissingUserId);

            return id;
        }
    }
}
