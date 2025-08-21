using IncidentReportingSystem.Application.Features.IncidentReports.Dtos;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using MediatR;

namespace IncidentReportingSystem.Application.Features.IncidentReports.Queries.GetIncidentStatistics;

public class GetIncidentStatisticsQueryHandler : IRequestHandler<GetIncidentStatisticsQuery, IncidentStatisticsDto>
{
    private readonly IIncidentReportRepository _repository;

    public GetIncidentStatisticsQueryHandler(IIncidentReportRepository repository)
    {
        _repository = repository;
    }

    public async Task<IncidentStatisticsDto> Handle(GetIncidentStatisticsQuery request, CancellationToken cancellationToken)
    {
        var allIncidents = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);

        return new IncidentStatisticsDto
        {
            TotalIncidents = allIncidents.Count(),
            IncidentsByCategory = allIncidents
                .GroupBy(i => i.Category.ToString())
                .ToDictionary(g => g.Key, g => g.Count()),

            IncidentsBySeverity = allIncidents
                .GroupBy(i => i.Severity.ToString())
                .ToDictionary(g => g.Key, g => g.Count()),

            IncidentsByStatus = allIncidents
                .GroupBy(i => i.Status.ToString())
                .ToDictionary(g => g.Key, g => g.Count()),
        };
    }
}
