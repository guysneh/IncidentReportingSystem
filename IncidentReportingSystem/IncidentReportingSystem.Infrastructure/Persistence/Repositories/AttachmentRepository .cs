using System;
using System.Threading;
using System.Threading.Tasks;
using IncidentReportingSystem.Application.Abstractions.Attachments;
using IncidentReportingSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IncidentReportingSystem.Infrastructure.Persistence.Repositories
{
    /// <summary>EF-based repository for the Attachment aggregate.</summary>
    public sealed class AttachmentRepository : IAttachmentRepository
    {
        private readonly ApplicationDbContext _db;
        public AttachmentRepository(ApplicationDbContext db) => _db = db;

        public async Task AddAsync(Attachment entity, CancellationToken ct)
        {
            await _db.Attachments.AddAsync(entity, ct).ConfigureAwait(false);
        }

        public Task<Attachment?> GetAsync(Guid id, CancellationToken ct)
        {
            return _db.Attachments.FirstOrDefaultAsync(a => a.Id == id, ct);
        }

        public Task<Attachment?> GetReadOnlyAsync(Guid id, CancellationToken ct)
        {
            return _db.Attachments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
        }
    }
}
