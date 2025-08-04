using FluentValidation;
using IncidentReportingSystem.Application.IncidentReports.Commands;
using IncidentReportingSystem.Application.IncidentReports.Commands.UpdateIncidentStatus;
using IncidentReportingSystem.Domain.Enums;

namespace IncidentReportingSystem.Application.IncidentReports.Validators;

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
