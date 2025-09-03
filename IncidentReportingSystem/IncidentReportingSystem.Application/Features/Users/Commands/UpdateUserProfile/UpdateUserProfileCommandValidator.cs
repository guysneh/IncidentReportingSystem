using FluentValidation;
using System.Text.RegularExpressions;

namespace IncidentReportingSystem.Application.Features.Users.Commands.UpdateUserProfile
{
    /// <summary>
    /// Validation rules for profile update:
    /// - Required
    /// - Length 1–50
    /// - Allowed: letters from any language, spaces, apostrophe (') and hyphen (-)
    /// - Values are trimmed (extra space collapsing is done in the handler)
    /// </summary>
    public sealed class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
    {
        private static readonly Regex NameRx =
            new(@"^[\p{L}][\p{L}\p{Zs}'-]{0,49}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public UpdateUserProfileCommandValidator()
        {
            RuleFor(x => x.FirstName)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("First name is required.")
                .Must(v => !string.IsNullOrEmpty(v) && v.Trim().Length is >= 1 and <= 50)
                    .WithMessage("First name must be 1–50 characters.")
                .Must(v => !string.IsNullOrEmpty(v) && NameRx.IsMatch(v.Trim()))
                    .WithMessage("First name contains invalid characters.");

            RuleFor(x => x.LastName)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Last name is required.")
                .Must(v => !string.IsNullOrEmpty(v) && v.Trim().Length is >= 1 and <= 50)
                    .WithMessage("Last name must be 1–50 characters.")
                .Must(v => !string.IsNullOrEmpty(v) && NameRx.IsMatch(v.Trim()))
                    .WithMessage("Last name contains invalid characters.");

        }
    }
}
