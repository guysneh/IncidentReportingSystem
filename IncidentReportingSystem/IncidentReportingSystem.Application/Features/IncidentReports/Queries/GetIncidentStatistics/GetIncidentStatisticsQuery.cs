using IncidentReportingSystem.Application.Features.IncidentReports.Dtos;
using MediatR;

namespace IncidentReportingSystem.Application.Features.IncidentReports.Queries.GetIncidentStatistics;

/// <summary>
/// Query to retrieve aggregated statistics about incident reports.
/// </summary>
public record GetIncidentStatisticsQuery() : IRequest<IncidentStatisticsDto>;