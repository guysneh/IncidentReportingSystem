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

namespace IncidentReportingSystem.Infrastructure.IncidentReports.Repositories
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
            CancellationToken cancellationToken = default)
        {
            IQueryable<IncidentReport> query = _context.IncidentReports.AsQueryable();

            // Filter out closed if not requested
            if (status.HasValue)
                query = query.Where(r => r.Status == status.Value);

            // Filter by category
            if (category.HasValue)
            {
                query = query.Where(i => i.Category == category.Value);
            }

            // Filter by severity
            if (severity.HasValue)
            {
                query = query.Where(i => i.Severity == severity.Value);
            }

            // Text search (description or location)
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var lowerSearch = searchText.ToLower();
                query = query.Where(i =>
                    i.Description.ToLower().Contains(lowerSearch) ||
                    i.Location.ToLower().Contains(lowerSearch));
            }

            // Filter by reported date
            if (reportedAfter.HasValue)
            {
                query = query.Where(i => i.ReportedAt >= reportedAfter.Value);
            }

            if (reportedBefore.HasValue)
            {
                query = query.Where(i => i.ReportedAt <= reportedBefore.Value);
            }

            // Paging and sorting
            return await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<IncidentReport>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.IncidentReports.ToListAsync(cancellationToken);
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
    }
}
