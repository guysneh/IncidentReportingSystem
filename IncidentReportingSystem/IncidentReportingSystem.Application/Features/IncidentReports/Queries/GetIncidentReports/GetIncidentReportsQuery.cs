using IncidentReportingSystem.Application.Common.Models;
using IncidentReportingSystem.Application.Features.IncidentReports.Dtos;
using IncidentReportingSystem.Application.Persistence;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;
using MediatR;

namespace IncidentReportingSystem.Application.Features.IncidentReports.Queries.GetIncidentReports;

/// <summary>
/// Query for retrieving incident reports with optional filters and paging.
/// </summary>
public record GetIncidentReportsQuery(
    /// <summary>
    /// Optional filter for incident status.
    /// </summary>
    IncidentStatus? Status = null,

    /// <summary>
    /// Number of records to skip for paging.
    /// </summary>
    int Skip = 0,

    /// <summary>
    /// Number of records to take for paging.
    /// </summary>
    int Take = 50,

    /// <summary>
    /// Optional category filter.
    /// </summary>
    IncidentCategory? Category = null,

    /// <summary>
    /// Optional severity filter.
    /// </summary>
    IncidentSeverity? Severity = null,

    /// <summary>
    /// Optional keyword to search in the description or location.
    /// </summary>
    string? SearchText = null,

    /// <summary>
    /// Optional filter for reports created after a specific date.
    /// </summary>
    DateTime? ReportedAfter = null,

    /// <summary>
    /// Optional filter for reports created before a specific date.
    /// </summary>
    DateTime? ReportedBefore = null,

    IncidentSortField SortBy = IncidentSortField.CreatedAt,

    SortDirection Direction = SortDirection.Desc
) : IRequest<PagedResult<IncidentReportDto>>;
