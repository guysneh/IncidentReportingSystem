using FluentValidation;

namespace IncidentReportingSystem.Application.Features.IncidentReports.Queries.GetIncidentReports
{
    public sealed class GetIncidentReportsQueryValidator : AbstractValidator<GetIncidentReportsQuery>
    {
        public GetIncidentReportsQueryValidator()
        {
            RuleFor(x => x.Skip).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Take).InclusiveBetween(1, 200);
            When(x => x.ReportedAfter.HasValue && x.ReportedBefore.HasValue, () =>
            {
                RuleFor(x => x).Must(x => x.ReportedAfter <= x.ReportedBefore)
                    .WithMessage("ReportedAfter must be <= ReportedBefore.");
            });
        }
    }
}
