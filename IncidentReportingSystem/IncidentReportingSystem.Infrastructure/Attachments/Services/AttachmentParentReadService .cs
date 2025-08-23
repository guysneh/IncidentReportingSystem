using IncidentReportingSystem.Application.Abstractions.Attachments;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;
using IncidentReportingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Infrastructure.Attachments.Services
{
    /// <summary>EF-backed parent existence checker for attachments.</summary>
    public sealed class AttachmentParentReadService : IAttachmentParentReadService
    {
        private readonly ApplicationDbContext _db;
        public AttachmentParentReadService(ApplicationDbContext db) => _db = db;

        public Task<bool> ExistsAsync(AttachmentParentType parentType, Guid parentId, CancellationToken ct)
        {
            return parentType switch
            {
                AttachmentParentType.Incident => _db.IncidentReports.AnyAsync(i => i.Id == parentId, ct),
                AttachmentParentType.Comment => _db.IncidentComments.AnyAsync(c => c.Id == parentId, ct),
                _ => Task.FromResult(false)
            };
        }
    }
}
