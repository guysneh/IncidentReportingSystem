using System;
using System.Threading;
using System.Threading.Tasks;
using IncidentReportingSystem.Application.Abstractions.Attachments;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace IncidentReportingSystem.Infrastructure.Persistence.Repositories
{
    /// <summary>EF-based repository for the Attachment aggregate.</summary>
    public sealed class AttachmentRepository : IAttachmentRepository
    {
        private readonly ApplicationDbContext _db;
        public AttachmentRepository(ApplicationDbContext db) => _db = db;

        public async Task AddAsync(Attachment entity, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(entity);
            await _db.Attachments.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        }

        public Task<Attachment?> GetAsync(Guid id, CancellationToken cancellationToken)
            => _db.Attachments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        public Task<Attachment?> GetReadOnlyAsync(Guid id, CancellationToken cancellationToken)
            => _db.Attachments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        public async Task<(IReadOnlyList<Attachment> Items, int Total)> ListByParentAsync(
            AttachmentParentType parentType,
            Guid parentId,
            int skip,
            int take,
            CancellationToken cancellationToken)
        {
            if (skip < 0) skip = 0;
            if (take <= 0) take = 100;

            var query = _db.Attachments.AsNoTracking()
                .Where(a => a.ParentType == parentType && a.ParentId == parentId);

            var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);

            var items = await query
                .OrderByDescending(a => a.CreatedAt) // newest-first
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return (items, total);
        }
    }
}
