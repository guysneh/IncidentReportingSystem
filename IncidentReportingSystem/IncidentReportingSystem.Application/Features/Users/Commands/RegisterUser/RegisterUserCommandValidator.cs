using System.Text.RegularExpressions;
using FluentValidation;
using IncidentReportingSystem.Application.Features.Users.Commands.RegisterUser;

namespace IncidentReportingSystem.Application.Users.Commands.RegisterUser
{
    public sealed partial class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
    {
        [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
        private static partial Regex EmailRegex();

        public RegisterUserCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .MaximumLength(320)
                .Matches(EmailRegex()).WithMessage("Invalid email format.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8);

            RuleFor(x => x.Roles)
                .NotNull()
                .Must(HaveAtLeastOneRole).WithMessage("At least one role is required.")
                .Must(AllRolesAllowed).WithMessage("One or more roles are invalid.");

            RuleFor(x => x.FirstName)
                .MaximumLength(100)
                .When(x => !string.IsNullOrWhiteSpace(x.FirstName));

            RuleFor(x => x.LastName)
                .MaximumLength(100)
                .When(x => !string.IsNullOrWhiteSpace(x.LastName));
        }

        private static bool HaveAtLeastOneRole(IEnumerable<string> roles) =>
            roles is not null && roles.Any();

        private static bool AllRolesAllowed(IEnumerable<string> roles) =>
            roles is not null && roles.All(r => Domain.Roles.Allowed.Contains(r));
    }
}
