using IncidentReportingSystem.Application.IncidentReports.Queries.GetIncidentReports;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;
using IncidentReportingSystem.Domain.Interfaces;
using IncidentReportingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Repository implementation for managing IncidentReport entities using EF Core.
    /// </summary>
    public class IncidentReportRepository : IIncidentReportRepository
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="IncidentReportRepository"/> class.
        /// </summary>
        /// <param name="context">EF Core database context.</param>
        public IncidentReportRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public async Task<IncidentReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.IncidentReports
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<IncidentReport>> GetAsync(
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
            CancellationToken cancellationToken = default)
        {
            IQueryable<IncidentReport> query = _context.IncidentReports.AsNoTracking();

            if (status.HasValue) query = query.Where(i => i.Status == status.Value);
            if (category.HasValue) query = query.Where(i => i.Category == category.Value);
            if (severity.HasValue) query = query.Where(i => i.Severity == severity.Value);
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var s = searchText.Trim().ToLower();
                query = query.Where(i =>
                    i.Description.ToLower().Contains(s) ||
                    i.Location.ToLower().Contains(s) ||
                    i.ReporterId.ToString().Contains(s));
            }
            if (reportedAfter.HasValue) query = query.Where(i => i.ReportedAt >= reportedAfter.Value);
            if (reportedBefore.HasValue) query = query.Where(i => i.ReportedAt <= reportedBefore.Value);

            query = (sortBy, direction) switch
            {
                (IncidentSortField.ReportedAt, SortDirection.Desc) => query.OrderByDescending(i => i.ReportedAt),
                (IncidentSortField.ReportedAt, SortDirection.Asc) => query.OrderBy(i => i.ReportedAt),
                (IncidentSortField.CreatedAt, SortDirection.Desc) => query.OrderByDescending(i => i.CreatedAt),
                (IncidentSortField.CreatedAt, SortDirection.Asc) => query.OrderBy(i => i.CreatedAt),
                (IncidentSortField.Severity, SortDirection.Desc) => query.OrderByDescending(i => i.Severity),
                (IncidentSortField.Severity, SortDirection.Asc) => query.OrderBy(i => i.Severity),
                (IncidentSortField.Status, SortDirection.Desc) => query.OrderByDescending(i => i.Status),
                (IncidentSortField.Status, SortDirection.Asc) => query.OrderBy(i => i.Status),
                _ => query.OrderByDescending(i => i.CreatedAt)
            };

            return await query.Skip(skip).Take(take).ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<IncidentReport>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.IncidentReports.ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task SaveAsync(IncidentReport report, CancellationToken cancellationToken = default)
        {
            var exists = await _context.IncidentReports.AnyAsync(i => i.Id == report.Id, cancellationToken).ConfigureAwait(false);

            if (exists)
            {
                _context.IncidentReports.Update(report);
            }
            else
            {
                await _context.IncidentReports.AddAsync(report, cancellationToken).ConfigureAwait(false);
            }

            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<(int UpdatedCount, List<Guid> NotFound)> BulkUpdateStatusAsync(
            IReadOnlyList<Guid> ids,
            IncidentStatus newStatus,
            CancellationToken ct)
        {
            if (ids is null || ids.Count == 0) return (0, new List<Guid>());

            var incidents = await _context.IncidentReports.Where(i => ids.Contains(i.Id)).ToListAsync(ct);
            var foundIds = incidents.Select(i => i.Id).ToHashSet();
            var notFound = ids.Where(id => !foundIds.Contains(id)).ToList();

            foreach (var inc in incidents)
            {
                inc.UpdateStatus(newStatus);
                _context.IncidentReports.Update(inc);
            }

            var updated = await _context.SaveChangesAsync(ct);
            return (updated, notFound);
        }

        /// <inheritdoc />
        public async Task TouchModifiedAtAsync(Guid incidentId, DateTime utcNow, CancellationToken ct)
        {
            var entity = await _context.IncidentReports.FirstOrDefaultAsync(x => x.Id == incidentId, ct);
            if (entity is null)
                throw new KeyNotFoundException($"Incident {incidentId} not found.");

            // Adjust property name to your entity ( ModifiedAt )
            entity.SetModifiedAt(utcNow);
            // No SaveChanges here; the caller's unit of work will commit.
        }
    }
}
