using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Abstractions.Security;
using IncidentReportingSystem.Application.Common.Exceptions;
using MediatR;
using System.Collections.Generic;

namespace IncidentReportingSystem.Application.Features.Users.Commands.LoginUser
{
    /// <summary>
    /// Verifies credentials and issues a JWT through the <see cref="IJwtTokenService"/> port.
    /// Enriches the token with OIDC-style name claims built from the persisted user profile.
    /// </summary>
    public sealed class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, LoginResultDto>
    {
        private readonly IUserRepository _users;
        private readonly IPasswordHasher _hasher;
        private readonly IJwtTokenService _jwt;

        public LoginUserCommandHandler(IUserRepository users, IPasswordHasher hasher, IJwtTokenService jwt)
        {
            _users = users;
            _hasher = hasher;
            _jwt = jwt;
        }

        /// <inheritdoc />
        public async Task<LoginResultDto> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var normalized = request.Email.Trim().ToUpperInvariant();
            var user = await _users.FindByNormalizedEmailAsync(normalized, cancellationToken).ConfigureAwait(false);
            if (user is null)
                throw new InvalidCredentialsException();

            if (!_hasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
                throw new InvalidCredentialsException();

            // Build OIDC-style claims from persisted profile
            var extra = new Dictionary<string, string>(StringComparer.Ordinal);
            if (!string.IsNullOrWhiteSpace(user.FirstName))
                extra["given_name"] = user.FirstName!;
            if (!string.IsNullOrWhiteSpace(user.LastName))
                extra["family_name"] = user.LastName!;
            if (!string.IsNullOrWhiteSpace(user.DisplayName))
                extra["name"] = user.DisplayName!; // JwtTokenService will fallback to email if not provided

            var (token, expiresAtUtc) = _jwt.Generate(
                userId: user.Id.ToString(),
                roles: user.Roles,
                email: user.Email,
                extraClaims: extra);

            return new LoginResultDto(token, expiresAtUtc);
        }
    }
}
