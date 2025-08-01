using IncidentReportingSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<IReadOnlyList<IncidentReport>> GetAsync(bool includeClosed = false, int skip = 0, int take = 50, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves or updates an incident.
        /// </summary>
        /// <param name="report">Incident to persist.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SaveAsync(IncidentReport report, CancellationToken cancellationToken = default);
    }
}
