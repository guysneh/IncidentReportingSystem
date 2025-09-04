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

            // Enforce exactly one allowed role at registration time
            RuleFor(x => x.Roles)
                .NotNull().WithMessage("Roles collection must be provided.")
                .Must(r => NormalizeRoles(r).Length == 1)
                    .WithMessage("Exactly one role must be provided.")
                .Must(r => {
                    var norm = NormalizeRoles(r);
                    return norm.Length == 1 && Domain.Roles.Allowed.Contains(norm[0]);
                })
                    .WithMessage("Provided role is invalid.");

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

        /// <summary>
        /// Returns the normalized (trimmed, case-insensitive distinct) set of roles.
        /// </summary>
        private static string[] NormalizeRoles(IEnumerable<string>? roles) =>
            (roles ?? Array.Empty<string>())
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
    }
}
