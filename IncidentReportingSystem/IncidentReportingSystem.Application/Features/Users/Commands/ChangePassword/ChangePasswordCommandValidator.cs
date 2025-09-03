// Application/Features/Users/Commands/ChangePassword/ChangePasswordCommandValidator.cs
using FluentValidation;

namespace IncidentReportingSystem.Application.Features.Users.Commands.ChangePassword;

/// <summary>
/// Strong password policy:
/// - 12–128 chars
/// - at least one lowercase, uppercase, digit, special
/// - must differ from current
/// </summary>
public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty();

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(12)
            .MaximumLength(128)
            .Must(HasLower).WithMessage("NewPassword must contain a lowercase letter.")
            .Must(HasUpper).WithMessage("NewPassword must contain an uppercase letter.")
            .Must(HasDigit).WithMessage("NewPassword must contain a digit.")
            .Must(HasSpecial).WithMessage("NewPassword must contain a special character.")
            .Must((cmd, np) => !string.Equals(np, cmd.CurrentPassword, StringComparison.Ordinal))
            .WithMessage("NewPassword must be different from CurrentPassword.");
    }

    private static bool HasLower(string s) => s.Any(char.IsLower);
    private static bool HasUpper(string s) => s.Any(char.IsUpper);
    private static bool HasDigit(string s) => s.Any(char.IsDigit);
    private static bool HasSpecial(string s) => s.Any(ch => !char.IsLetterOrDigit(ch));
}
