// Application/Features/Users/Commands/ChangePassword/ChangePasswordCommand.cs
using MediatR;

namespace IncidentReportingSystem.Application.Features.Users.Commands.ChangePassword;

/// <summary>
/// Changes the authenticated user's password.
/// </summary>
public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest<Unit>;
