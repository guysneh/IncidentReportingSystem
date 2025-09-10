using FluentValidation;

namespace  IncidentReportingSystem.Application.Features.Statistics.Contracts;

public sealed class GetIncidentSeriesQueryValidator : AbstractValidator<GetIncidentSeriesQuery>
{
    public GetIncidentSeriesQueryValidator()
    {
        RuleFor(x => x.From).LessThan(x => x.To);
        RuleFor(x => x.To).GreaterThan(x => x.From);
    }
}
