using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Abstractions.Security;
using IncidentReportingSystem.Application.Common.Exceptions;
using MediatR;

namespace IncidentReportingSystem.Application.Features.Users.Commands.UpdateUserProfile
{
    /// <summary>
    /// Loads the current user, applies normalized names using a domain method,
    /// and persists the change through the unit of work.
    /// </summary>
    public sealed class UpdateUserProfileCommandHandler
        : IRequestHandler<UpdateUserProfileCommand, UserProfileDto>
    {
        private readonly IUserRepository _users;
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _current;

        public UpdateUserProfileCommandHandler(
            IUserRepository users,
            IUnitOfWork uow,
            ICurrentUserService current)
        {
            _users = users ?? throw new ArgumentNullException(nameof(users));
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _current = current ?? throw new ArgumentNullException(nameof(current));
        }

        public async Task<UserProfileDto> Handle(UpdateUserProfileCommand request, CancellationToken ct)
        {
            var userId = _current.UserIdOrThrow();

            var user = await _users.GetByIdAsync(userId, ct)
                       ?? throw new NotFoundException("User not found.");

            static string Normalize(string s) =>
                string.Join(' ', (s ?? string.Empty).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));

            var first = Normalize(request.FirstName);
            var last = Normalize(request.LastName);

            user.UpdateNames(first, last); // domain method (sets ModifiedAtUtc, DisplayName, raises audit if needed)

            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

            return new UserProfileDto(
                user.Id,
                user.Email,
                user.FirstName ?? string.Empty,
                user.LastName ?? string.Empty,
                user.DisplayName ?? string.Empty,
                user.CreatedAtUtc,
                user.ModifiedAtUtc
            );
        }
    }
}
