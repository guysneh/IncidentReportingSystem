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
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<IncidentReport>> GetAsync(bool includeClosed = false, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
        {
            IQueryable<IncidentReport> query = _context.IncidentReports;

            if (!includeClosed)
            {
                query = query.Where(i => i.Status != IncidentStatus.Closed);
            }

            return await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task SaveAsync(IncidentReport report, CancellationToken cancellationToken = default)
        {
            var exists = await _context.IncidentReports.AnyAsync(i => i.Id == report.Id, cancellationToken);

            if (exists)
            {
                _context.IncidentReports.Update(report);
            }
            else
            {
                await _context.IncidentReports.AddAsync(report, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
