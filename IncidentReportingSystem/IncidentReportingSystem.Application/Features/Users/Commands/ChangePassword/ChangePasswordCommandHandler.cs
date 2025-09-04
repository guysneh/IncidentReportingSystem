// Application/Features/Users/Commands/ChangePassword/ChangePasswordCommandHandler.cs
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Abstractions.Security;
using IncidentReportingSystem.Application.Common.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IncidentReportingSystem.Application.Features.Users.Commands.ChangePassword;

/// <summary>
/// Verifies the current password, hashes the new one and persists to the user entity.
/// Throws ForbiddenException when current password is invalid.
/// </summary>
public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Unit>
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _current;
    private readonly IPasswordHasher _passwords;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    public ChangePasswordCommandHandler(
        IUserRepository users,
        IUnitOfWork uow,
        ICurrentUserService current,
        IPasswordHasher passwords,
        ILogger<ChangePasswordCommandHandler> logger)
    {
        _users = users;
        _uow = uow;
        _current = current;
        _passwords = passwords;
        _logger = logger;
    }

    public async Task<Unit> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var userId = _current.UserIdOrThrow();

        var user = await _users.GetByIdAsync(userId, ct)
                   ?? throw new NotFoundException("User not found.");

        // Verify current password
        var ok = _passwords.Verify(request.CurrentPassword, user.PasswordHash, user.PasswordSalt);
        if (!ok)
            throw new ForbiddenException("Invalid credentials."); // mapped to 403 by middleware

        // Hash new password
        var (hash, salt) = _passwords.HashPassword(request.NewPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.ModifiedAtUtc = DateTime.UtcNow;

        await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation("User password changed. UserId={UserId}", user.Id);
        return Unit.Value;
    }
}
