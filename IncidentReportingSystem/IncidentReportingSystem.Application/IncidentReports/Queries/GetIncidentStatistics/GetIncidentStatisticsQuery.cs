using IncidentReportingSystem.Application.IncidentReports.DTOs;
using MediatR;

namespace IncidentReportingSystem.Application.IncidentReports.Queries.GetIncidentStatistics;

/// <summary>
/// Query to retrieve aggregated statistics about incident reports.
/// </summary>
public record GetIncidentStatisticsQuery() : IRequest<IncidentStatisticsDto>;