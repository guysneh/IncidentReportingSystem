using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;

namespace IncidentReportingSystem.Domain.Interfaces
{
    /// <summary>
    /// Defines contract for data access of incident reports.
    /// </summary>
    public interface IIncidentReportRepository
    {
        /// <summary>
        /// Retrieves an incident by ID.
        /// </summary>
        /// <param name="id">Unique incident ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<IncidentReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves incidents with optional filtering.
        /// </summary>
        /// <param name="includeClosed">Whether to include closed incidents.</param>
        /// <param name="skip">Number of incidents to skip (for paging).</param>
        /// <param name="take">Number of incidents to take (for paging).</param>
        /// <param name="category">Optional category filter.</param>
        /// <param name="severity">Optional severity filter.</param>
        /// <param name="searchText">Optional free-text search (in description or location).</param>
        /// <param name="reportedAfter">Filter incidents reported after this date.</param>
        /// <param name="reportedBefore">Filter incidents reported before this date.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<IReadOnlyList<IncidentReport>> GetAsync(
            bool includeClosed = false,
            int skip = 0,
            int take = 50,
            IncidentCategory? category = null,
            IncidentSeverity? severity = null,
            string? searchText = null,
            DateTime? reportedAfter = null,
            DateTime? reportedBefore = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves or updates an incident.
        /// </summary>
        /// <param name="report">Incident to persist.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SaveAsync(IncidentReport report, CancellationToken cancellationToken = default);
    }
}
