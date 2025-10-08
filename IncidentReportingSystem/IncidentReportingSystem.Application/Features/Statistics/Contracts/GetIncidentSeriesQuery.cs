using MediatR;

namespace IncidentReportingSystem.Application.Features.Statistics.Contracts;

public enum TimeGranularity { Day, Week }

public sealed record IncidentSeriesPoint(string Label, int Count);

public sealed record GetIncidentSeriesQuery(
    DateTime From,
    DateTime To,
    TimeGranularity Granularity
) : IRequest<IReadOnlyList<IncidentSeriesPoint>>;
