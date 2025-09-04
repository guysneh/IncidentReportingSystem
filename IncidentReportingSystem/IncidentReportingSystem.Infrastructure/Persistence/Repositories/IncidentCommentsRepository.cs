using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Common.Models;
using IncidentReportingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IncidentReportingSystem.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// EF Core implementation of <see cref="IIncidentCommentsRepository"/> backed by <see cref="ApplicationDbContext"/>.
    /// </summary>
    public sealed class IncidentCommentsRepository : IIncidentCommentsRepository
    {
        private readonly ApplicationDbContext _db;
        public IncidentCommentsRepository(ApplicationDbContext db) => _db = db;

        /// <inheritdoc />
        public Task<bool> IncidentExistsAsync(Guid incidentId, CancellationToken cancellationToken) =>
            _db.IncidentReports.AsNoTracking().AnyAsync(i => i.Id == incidentId, cancellationToken);

        /// <inheritdoc />
        public Task<IncidentComment?> GetAsync(Guid incidentId, Guid commentId, CancellationToken cancellationToken) =>
            _db.IncidentComments.FirstOrDefaultAsync(x => x.IncidentId == incidentId && x.Id == commentId, cancellationToken);

        /// <inheritdoc />
        public async Task<IReadOnlyList<IncidentComment>> ListAsync(Guid incidentId, int skip, int take, CancellationToken cancellationToken) =>
            await _db.IncidentComments.AsNoTracking()
                .Where(x => x.IncidentId == incidentId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Skip(Math.Max(0, skip))
                .Take(take <= 0 ? 50 : take)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

        /// <inheritdoc />
        public Task<IncidentComment> AddAsync(IncidentComment c, CancellationToken cancellationToken)
        {
            _db.IncidentComments.Add(c);
            return Task.FromResult(c);
        }

        /// <inheritdoc />
        public Task RemoveAsync(IncidentComment c, CancellationToken cancellationToken)
        {
            _db.IncidentComments.Remove(c);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task<PagedResult<IncidentComment>> ListPagedAsync(Guid incidentId, int skip, int take, CancellationToken cancellationToken)
        {
            var query = _db.IncidentComments.AsNoTracking()
                .Where(x => x.IncidentId == incidentId);

            var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);

            var items = await query
                .OrderByDescending(x => x.CreatedAtUtc)
                .Skip(Math.Max(0, skip))
                .Take(take <= 0 ? 50 : take)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            return new PagedResult<IncidentComment>(items, total, skip, take);
        }
    }
}