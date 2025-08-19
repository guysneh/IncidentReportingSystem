using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Interfaces;
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
        public Task<bool> IncidentExistsAsync(Guid incidentId, CancellationToken ct) =>
            _db.IncidentReports.AsNoTracking().AnyAsync(i => i.Id == incidentId, ct);

        /// <inheritdoc />
        public Task<IncidentComment?> GetAsync(Guid incidentId, Guid commentId, CancellationToken ct) =>
            _db.IncidentComments.FirstOrDefaultAsync(x => x.IncidentId == incidentId && x.Id == commentId, ct);

        /// <inheritdoc />
        public async Task<IReadOnlyList<IncidentComment>> ListAsync(Guid incidentId, int skip, int take, CancellationToken ct) =>
            await _db.IncidentComments.AsNoTracking()
                .Where(x => x.IncidentId == incidentId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Skip(Math.Max(0, skip))
                .Take(take <= 0 ? 50 : take)
                .ToListAsync(ct);

        /// <inheritdoc />
        public Task<IncidentComment> AddAsync(IncidentComment c, CancellationToken ct)
        {
            _db.IncidentComments.Add(c);
            return Task.FromResult(c);
        }

        /// <inheritdoc />
        public Task RemoveAsync(IncidentComment c, CancellationToken ct)
        {
            _db.IncidentComments.Remove(c);
            return Task.CompletedTask;
        }
    }
}