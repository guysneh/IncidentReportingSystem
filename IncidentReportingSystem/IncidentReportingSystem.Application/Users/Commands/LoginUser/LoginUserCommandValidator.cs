using FluentValidation;

namespace IncidentReportingSystem.Application.Users.Commands.LoginUser
{
    /// <summary>
    /// Validator for LoginUserCommand.
    /// </summary>
    public sealed class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
    {
        public LoginUserCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress();

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8);
        }
    }
}