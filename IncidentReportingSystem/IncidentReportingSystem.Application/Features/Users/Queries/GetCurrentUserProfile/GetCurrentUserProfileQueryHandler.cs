using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Abstractions.Security;
using IncidentReportingSystem.Application.Common.Exceptions;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Application.Features.Users.Queries.GetCurrentUserProfile
{
    /// <summary>
    /// Loads the current user from persistence and maps to a DTO.
    /// No web concerns here; pure application logic.
    /// </summary>
    public sealed class GetCurrentUserProfileQueryHandler
        : IRequestHandler<GetCurrentUserProfileQuery, CurrentUserProfileDto>
    {
        private readonly IUserRepository _users;
        private readonly ICurrentUserService _current;

        public GetCurrentUserProfileQueryHandler(IUserRepository users, ICurrentUserService current)
        {
            _users = users ?? throw new ArgumentNullException(nameof(users));
            _current = current ?? throw new ArgumentNullException(nameof(current));
        }

        public async Task<CurrentUserProfileDto> Handle(GetCurrentUserProfileQuery request, CancellationToken ct)
        {
            var userId = _current.UserIdOrThrow();
            var user = await _users.GetByIdAsync(userId, ct).ConfigureAwait(false)
                       ?? throw new NotFoundException("User not found.");

            var roles = (user.Roles ?? Array.Empty<string>())
                        .Select(r => r.ToString())
                        .ToArray();

            var display = string.Join(" ",
                new[] { user.FirstName, user.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));

            return new CurrentUserProfileDto(
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                roles,
                display
            );
        }
    }
}
