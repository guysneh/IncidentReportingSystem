using IncidentReportingSystem.Application.Authentication;
using IncidentReportingSystem.Application.Common.Exceptions;
using IncidentReportingSystem.Domain.Auth;
using IncidentReportingSystem.Domain.Interfaces;
using IncidentReportingSystem.Domain.Entities;
using MediatR;

namespace IncidentReportingSystem.Application.Users.Commands.RegisterUser
{
    /// <summary>
    /// Handles the registration of a new user with hashed password and assigned roles.
    /// Clean Architecture: depends only on Domain interfaces/services (no EF here).
    /// </summary>
    public sealed class RegisterUserCommandHandler
        : IRequestHandler<RegisterUserCommand, RegisterUserResult>
    {
        private readonly IUserRepository _users;
        private readonly IUnitOfWork _uow;
        private readonly IPasswordHasher _hasher;

        public RegisterUserCommandHandler(IUserRepository users, IUnitOfWork uow, IPasswordHasher hasher)
        {
            _users = users ?? throw new ArgumentNullException(nameof(users));
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        }

        public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            var normalized = request.Email.Trim().ToUpperInvariant();

            if (await _users.ExistsByNormalizedEmailAsync(normalized, cancellationToken).ConfigureAwait(false))
                throw new EmailAlreadyExistsException(request.Email);

            if (!request.Roles.All(r => Roles.Allowed.Contains(r)))
                throw new ArgumentException("One or more roles are invalid.", nameof(request.Roles));

            var (hash, salt) = _hasher.HashPassword(request.Password);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email.Trim(),
                NormalizedEmail = normalized,
                PasswordHash = hash,
                PasswordSalt = salt,
                CreatedAtUtc = DateTime.UtcNow
            };
            user.SetRoles(request.Roles);

            await _users.AddAsync(user, cancellationToken).ConfigureAwait(false);
            await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new RegisterUserResult(user.Id, user.Email, user.Roles, user.CreatedAtUtc);
        }
    }
}
