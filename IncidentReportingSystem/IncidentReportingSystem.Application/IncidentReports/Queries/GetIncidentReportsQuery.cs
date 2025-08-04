using IncidentReportingSystem.Domain.Entities;
using MediatR;

namespace IncidentReportingSystem.Application.IncidentReports.Queries.GetIncidentReports
{
    /// <summary>
    /// Query for retrieving incident reports with optional filters and paging.
    /// </summary>
    public record GetIncidentReportsQuery(
        bool IncludeClosed = false,
        int Skip = 0,
        int Take = 50
    ) : IRequest<IReadOnlyList<IncidentReport>>;
}
