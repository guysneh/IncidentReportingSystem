using FluentValidation;

namespace IncidentReportingSystem.Application.IncidentReports.Commands.CreateIncidentReport;

/// <summary>
/// Validator for <see cref="CreateIncidentReportCommand"/>, ensuring required fields and business rules are met.
/// </summary>
public class CreateIncidentReportCommandValidator : AbstractValidator<CreateIncidentReportCommand>
{
    public CreateIncidentReportCommandValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required.");

        RuleFor(x => x.Location)
            .NotEmpty()
            .WithMessage("Location is required.");

        RuleFor(x => x.ReporterId)
            .NotEmpty()
            .WithMessage("Reporter ID is required.");

        RuleFor(x => x.Category)
            .IsInEnum()
            .WithMessage("Category must be a valid value.");

        RuleFor(x => x.Severity)
            .IsInEnum()
            .WithMessage("Severity must be a valid value.");
    }
}