using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Abstractions.Security;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain;
using MediatR;
using IncidentReportingSystem.Application.Common.Exceptions;


namespace IncidentReportingSystem.Application.Features.Users.Commands.RegisterUser
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
            ArgumentNullException.ThrowIfNull(request);

            var normalized = request.Email.Trim().ToUpperInvariant();

            if (await _users.ExistsByNormalizedEmailAsync(normalized, cancellationToken).ConfigureAwait(false))
                throw new EmailAlreadyExistsException(request.Email);

            // Normalize and enforce exactly one role
            var normalizedRoles = request.Roles?
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? Array.Empty<string>();

            if (normalizedRoles.Length != 1 || !normalizedRoles.All(r => Roles.Allowed.Contains(r)))
                throw new ArgumentException("Exactly one valid role is required.", nameof(request.Roles));

            var (hash, salt) = _hasher.HashPassword(request.Password);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email.Trim(),
                NormalizedEmail = normalized,
                PasswordHash = hash,
                PasswordSalt = salt,
                CreatedAtUtc = DateTime.UtcNow,
                FirstName = string.IsNullOrWhiteSpace(request.FirstName) ? null : request.FirstName.Trim(),
                LastName = string.IsNullOrWhiteSpace(request.LastName) ? null : request.LastName.Trim(),
            };

            // Compute DisplayName (card 6 behavior)
            user.DisplayName = (!string.IsNullOrWhiteSpace(user.FirstName) || !string.IsNullOrWhiteSpace(user.LastName))
                ? string.Join(" ", new[] { user.FirstName, user.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)))
                : user.Email;

            // Persist single role (DB stays text[] for future flexibility)
            user.SetRoles(normalizedRoles);

            await _users.AddAsync(user, cancellationToken).ConfigureAwait(false);
            await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new RegisterUserResult(user.Id, user.Email, user.Roles, user.CreatedAtUtc);
        }
    }
}
