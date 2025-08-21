using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Abstractions.Security;
using IncidentReportingSystem.Application.Exceptions;
using MediatR;

namespace IncidentReportingSystem.Application.Features.Users.Commands.LoginUser
{
    /// <summary>
    /// Verifies credentials and issues a JWT through the IJwtTokenService port.
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

        public async Task<LoginResultDto> Handle(LoginUserCommand request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var normalized = request.Email.Trim().ToUpperInvariant();
            var user = await _users.FindByNormalizedEmailAsync(normalized, ct).ConfigureAwait(false);
            if (user is null)
                throw new InvalidCredentialsException();
            if (!_hasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt, ct))
                throw new InvalidCredentialsException();
            var ok = _hasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt, ct);
            if (!ok)
                throw new InvalidCredentialsException();

            var (token, expiresAtUtc) = _jwt.Generate(user.Id.ToString(), user.Roles, user.Email);
            return new LoginResultDto(token, expiresAtUtc);
        }
    }
}