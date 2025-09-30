using IncidentReportingSystem.Application.Abstractions.Identity;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Common.Models;
using IncidentReportingSystem.Application.Features.IncidentReports.Dtos;
using MediatR;

namespace IncidentReportingSystem.Application.Features.IncidentReports.Queries.GetIncidentReports;

public sealed class GetIncidentReportsQueryHandler
    : IRequestHandler<GetIncidentReportsQuery, PagedResult<IncidentReportDto>>
{
    private readonly IIncidentReportRepository _repository;
    private readonly IUserDirectory _userDirectory;

    public GetIncidentReportsQueryHandler(
        IIncidentReportRepository repository,
        IUserDirectory userDirectory)
    {
        _repository = repository;
        _userDirectory = userDirectory;
    }

    public async Task<PagedResult<IncidentReportDto>> Handle(
        GetIncidentReportsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Use the repository signature that actually exists in your solution:
        // GetPagedAsync(status, skip, take, category, severity, searchText, reportedAfter, reportedBefore, sortBy, direction, ct)
        var page = await _repository.GetPagedAsync(
            status: request.Status,
            skip: request.Skip,
            take: request.Take,
            category: request.Category,
            severity: request.Severity,
            searchText: request.SearchText,
            reportedAfter: request.ReportedAfter,
            reportedBefore: request.ReportedBefore,
            sortBy: request.SortBy,
            direction: request.Direction,
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        // Batch resolve reporter display names
        var reporterIds = page.Items.Select(x => x.ReporterId).Distinct().ToArray();
        var nameMap = await _userDirectory.ResolveDisplayNamesAsync(reporterIds, cancellationToken)
                                          .ConfigureAwait(false);

        var mapped = page.Items.Select(e =>
        {
            nameMap.TryGetValue(e.ReporterId, out var dn);
            var display = string.IsNullOrWhiteSpace(dn) ? null : dn;
            return e.ToDto(display);
        }).ToList();

        return new PagedResult<IncidentReportDto>(mapped, page.Total, page.Skip, page.Take);
    }
}
