using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Features.Attachments.Queries.ListAttachmentsByParent;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace IncidentReportingSystem.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// EF-based repository for the Attachment aggregate. Implements full
    /// search/filter/sort/paging for listing attachments by parent.
    /// </summary>
    public sealed class AttachmentRepository : IAttachmentRepository
    {
        private readonly ApplicationDbContext _db;
        public AttachmentRepository(ApplicationDbContext db) => _db = db;

        /// <inheritdoc />
        public async Task AddAsync(Attachment entity, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(entity);
            await _db.Attachments.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<Attachment?> GetAsync(Guid id, CancellationToken cancellationToken)
            => _db.Attachments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        /// <inheritdoc />
        public Task<Attachment?> GetReadOnlyAsync(Guid id, CancellationToken cancellationToken)
            => _db.Attachments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        /// <inheritdoc />
        public async Task<(IReadOnlyList<Attachment> Items, int Total)> ListByParentAsync(
            AttachmentParentType parentType,
            Guid parentId,
            AttachmentListFilters f,
            CancellationToken cancellationToken)
        {
            var query = _db.Attachments.AsNoTracking()
                .Where(a => a.ParentType == parentType && a.ParentId == parentId);

            // Search by filename (provider-aware: Npgsql => ILIKE, otherwise case-insensitive Contains)
            if (!string.IsNullOrWhiteSpace(f.Search))
            {
                var isNpgsql = _db.Database.ProviderName?.IndexOf("Npgsql", StringComparison.OrdinalIgnoreCase) >= 0;
                if (isNpgsql)
                {
                    query = query.Where(a => EF.Functions.ILike(a.FileName, $"%{f.Search}%"));
                }
                else
                {
                    var s = f.Search.ToLowerInvariant();
                    query = query.Where(a => a.FileName.ToLower().Contains(s));
                }
            }

            if (!string.IsNullOrWhiteSpace(f.ContentType))
                query = query.Where(a => a.ContentType == f.ContentType);

            if (f.CreatedAfter.HasValue)
                query = query.Where(a => a.CreatedAt >= f.CreatedAfter.Value);

            if (f.CreatedBefore.HasValue)
                query = query.Where(a => a.CreatedAt <= f.CreatedBefore.Value);

            // ---- Sorting (filename is case-insensitive) ----
            var orderBy = (f.OrderBy ?? "createdAt").Trim().ToLowerInvariant();
            var direction = (f.Direction ?? "desc").Trim().ToLowerInvariant();

            IQueryable<Attachment> ordered = (orderBy, direction) switch
            {
                ("filename", "asc") => query.OrderBy(a => a.FileName.ToLower())
                                             .ThenBy(a => a.Id),
                ("filename", "desc") => query.OrderByDescending(a => a.FileName.ToLower())
                                             .ThenBy(a => a.Id),

                ("size", "asc") => query.OrderBy(a => a.Size)
                                             .ThenBy(a => a.Id),
                ("size", "desc") => query.OrderByDescending(a => a.Size)
                                             .ThenBy(a => a.Id),

                ("createdat", "asc") => query.OrderBy(a => a.CreatedAt)
                                             .ThenBy(a => a.Id),

                _ /* default: createdAt desc */
                                   => query.OrderByDescending(a => a.CreatedAt)
                                           .ThenBy(a => a.Id),
            };

            var total = await ordered.CountAsync(cancellationToken).ConfigureAwait(false);

            var take = f.Take <= 0 ? 100 : f.Take;
            var skip = f.Skip < 0 ? 0 : f.Skip;

            var items = await ordered
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return (items, total);
        }


        /// <inheritdoc />
        public Task RemoveAsync(Attachment entity, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(entity);
            _db.Attachments.Remove(entity);
            return Task.CompletedTask;
        }
    }
}
