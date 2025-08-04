using FluentValidation;
using IncidentReportingSystem.Application.IncidentReports.Commands;
using IncidentReportingSystem.Application.IncidentReports.Commands.CreateIncidentReport;

namespace IncidentReportingSystem.Application.IncidentReports.Validators;

/// <summary>
/// Validator for the CreateIncidentReportCommand.
/// Ensures all required fields are present and valid.
/// </summary>
public class CreateIncidentReportCommandValidator : AbstractValidator<CreateIncidentReportCommand>
{
    public CreateIncidentReportCommandValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(1000);

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("Location is required.")
            .MaximumLength(255);

        RuleFor(x => x.ReporterId)
            .NotEmpty().WithMessage("ReporterId is required.");

        RuleFor(x => x.SystemAffected)
            .MaximumLength(255);
    }
}
