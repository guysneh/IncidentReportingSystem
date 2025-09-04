using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Common.Models;                
using IncidentReportingSystem.Application.Features.IncidentReports.Dtos;
using IncidentReportingSystem.Application.Features.IncidentReports.Mappers;
using MediatR;

namespace IncidentReportingSystem.Application.Features.IncidentReports.Queries.GetIncidentReports
{
    /// <summary>Delegates retrieval to repository and maps to DTOs.</summary>
    public sealed class GetIncidentReportsQueryHandler : IRequestHandler<GetIncidentReportsQuery, PagedResult<IncidentReportDto>>
    {
        private readonly IIncidentReportRepository _repository;
        public GetIncidentReportsQueryHandler(IIncidentReportRepository repository) => _repository = repository;

        public async Task<PagedResult<IncidentReportDto>> Handle(GetIncidentReportsQuery request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

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

            var mapped = page.Items.Select(x => x.ToDto()).ToList();
            return new PagedResult<IncidentReportDto>(mapped, page.Total, page.Skip, page.Take);
        }
    }
}
