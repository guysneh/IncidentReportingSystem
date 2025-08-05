using IncidentReportingSystem.Application.IncidentReports.DTOs;
using IncidentReportingSystem.Domain.Interfaces;
using MediatR;

namespace IncidentReportingSystem.Application.IncidentReports.Queries.GetIncidentStatistics;

public class GetIncidentStatisticsQueryHandler : IRequestHandler<GetIncidentStatisticsQuery, IncidentStatisticsDto>
{
    private readonly IIncidentReportRepository _repository;

    public GetIncidentStatisticsQueryHandler(IIncidentReportRepository repository)
    {
        _repository = repository;
    }

    public async Task<IncidentStatisticsDto> Handle(GetIncidentStatisticsQuery request, CancellationToken cancellationToken)
    {
        var allIncidents = await _repository.GetAllAsync(cancellationToken);

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
