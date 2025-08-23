using FluentValidation;

namespace IncidentReportingSystem.Application.Features.IncidentReports.Commands.BulkUpdateIncidentStatus
{
    public sealed class BulkUpdateIncidentStatusValidator : AbstractValidator<BulkUpdateIncidentStatusCommand>
    {
        public BulkUpdateIncidentStatusValidator()
        {
            RuleFor(x => x.IdempotencyKey).NotEmpty().WithMessage("Idempotency-Key is required.");
            RuleFor(x => x.Ids).NotNull().Must(ids => ids?.Count > 0).WithMessage("Ids must be non-empty.");
        }
    }
}
