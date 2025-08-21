using FluentValidation;

namespace IncidentReportingSystem.Application.Features.IncidentReports.Commands.UpdateIncidentStatus;

/// <summary>
/// Validator for the UpdateIncidentStatusCommand.
/// Ensures that the incident ID is provided and the new status is a valid enum value.
/// </summary>
public class UpdateIncidentStatusValidator : AbstractValidator<UpdateIncidentStatusCommand>
{
    public UpdateIncidentStatusValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Incident ID is required.");

        RuleFor(x => x.NewStatus)
            .IsInEnum()
            .WithMessage("New status must be a valid IncidentStatus value.");
    }
}
