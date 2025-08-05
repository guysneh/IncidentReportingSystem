using IncidentReportingSystem.Domain.Entities;
using MediatR;

namespace IncidentReportingSystem.Application.IncidentReports.Queries.GetIncidentReportById
{
    /// <summary>
    /// Query for retrieving a specific incident report by its unique identifier.
    /// </summary>
    public record GetIncidentReportByIdQuery(Guid Id) : IRequest<IncidentReport>;
}