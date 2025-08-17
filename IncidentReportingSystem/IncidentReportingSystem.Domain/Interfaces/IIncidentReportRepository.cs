using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;

namespace IncidentReportingSystem.Domain.Interfaces
{
    public interface IIncidentReportRepository
    {
        Task<IReadOnlyList<IncidentReport>> GetAsync(
            IncidentStatus? status = null,
            int skip = 0,
            int take = 50,
            IncidentCategory? category = null,
            IncidentSeverity? severity = null,
            string? searchText = null,
            DateTime? reportedAfter = null,
            DateTime? reportedBefore = null,
            IncidentSortField sortBy = IncidentSortField.CreatedAt,
            SortDirection direction = SortDirection.Desc,
            CancellationToken cancellationToken = default);

        Task<(int UpdatedCount, List<Guid> NotFound)> BulkUpdateStatusAsync(
            IReadOnlyList<Guid> ids,
            IncidentStatus newStatus,
            CancellationToken ct);

        Task<IncidentReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<IncidentReport>> GetAllAsync(CancellationToken cancellationToken = default);
        Task SaveAsync(IncidentReport report, CancellationToken cancellationToken = default);
    }
}
