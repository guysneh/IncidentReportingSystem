using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using MediatR;

namespace IncidentReportingSystem.Application.Features.IncidentReports.Queries.GetIncidentReports
{
    /// <summary>Delegates retrieval to the repository, forwarding sort enums.</summary>
    public sealed class GetIncidentReportsQueryHandler : IRequestHandler<GetIncidentReportsQuery, IReadOnlyList<IncidentReport>>
    {
        private readonly IIncidentReportRepository _repository;
        public GetIncidentReportsQueryHandler(IIncidentReportRepository repository) => _repository = repository;

        public async Task<IReadOnlyList<IncidentReport>> Handle(GetIncidentReportsQuery request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var result = await _repository.GetAsync(
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

            return result ?? Array.Empty<IncidentReport>();
        }
    }
}
