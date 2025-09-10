using IncidentReportingSystem.Application.Features.Statistics.Contracts;

namespace IncidentReportingSystem.Application.Abstractions.Persistence;

public interface IIncidentReadRepository
{
    Task<IReadOnlyList<IncidentSeriesPoint>> GetIncidentSeriesAsync(
        DateTime from,
        DateTime to,
        TimeGranularity granularity,
        CancellationToken ct);
}
