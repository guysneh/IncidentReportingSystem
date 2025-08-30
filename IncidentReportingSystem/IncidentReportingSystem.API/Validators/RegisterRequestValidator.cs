using FluentValidation;
using IncidentReportingSystem.API.Contracts.Authentication;
using IncidentReportingSystem.Domain;

namespace IncidentReportingSystem.API.Validators
{
    public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty().MinimumLength(8);

            // Validate legacy Role if present
            RuleFor(x => x.Role)
                .Must(r => string.IsNullOrWhiteSpace(r) || new[] { Roles.User, Roles.Admin }.Contains(r))
                .WithMessage("Invalid role.");

            // Validate Roles[] if present
            When(x => x.Roles is { Length: > 0 }, () =>
            {
                RuleForEach(x => x.Roles!)
                    .Must(r => new[] { Roles.User, Roles.Admin }.Contains(r))
                    .WithMessage("Invalid role.");
            });
        }
    }
}
