using IncidentReportingSystem.Application.Abstractions.Attachments;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Common.Errors;
using IncidentReportingSystem.Application.Common.Exceptions;
using IncidentReportingSystem.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Application.Features.Attachments.Commands.AbortUploadAttachment
{
    /// <summary>
    /// Handles aborting a pending upload: verifies permissions, deletes any staged object from storage,
    /// removes the database row, and commits. Conflicts (non-pending) return 409 via exception mapping.
    /// </summary>
    public sealed class AbortUploadAttachmentCommandHandler : IRequestHandler<AbortUploadAttachmentCommand>
    {
        private static readonly EventId AuditEventId = new(20230, "AttachmentAbort");

        private readonly IAttachmentRepository _repo;
        private readonly IAttachmentStorage _storage;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<AbortUploadAttachmentCommandHandler> _logger;

        public AbortUploadAttachmentCommandHandler(
            IAttachmentRepository repo,
            IAttachmentStorage storage,
            IUnitOfWork uow,
            ILogger<AbortUploadAttachmentCommandHandler> logger)
        {
            _repo = repo;
            _storage = storage;
            _uow = uow;
            _logger = logger;
        }

        public async Task Handle(AbortUploadAttachmentCommand request, CancellationToken cancellationToken)
        {
            var a = await _repo.GetAsync(request.AttachmentId, cancellationToken).ConfigureAwait(false)
                ?? throw new NotFoundException(AttachmentErrors.AttachmentNotFound);

            var isOwner = a.UploadedBy == request.RequestedBy;
            if (!isOwner && !request.RequesterIsAdmin)
                throw new ForbiddenException("Only the uploader or an Admin may abort this upload.");

            if (a.Status != AttachmentStatus.Pending)
                throw new InvalidOperationException(AttachmentErrors.AttachmentNotPending);

            // Best-effort delete of any staged object; storage may or may not contain it.
            try
            {
                await _storage.DeleteAsync(a.StoragePath, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Swallow non-fatal storage exceptions: abort should proceed even if cleanup is partial.
                _logger.LogWarning(ex, "Storage cleanup failed for {AttachmentId} at {StoragePath}", a.Id, a.StoragePath);
            }

            await _repo.RemoveAsync(a, cancellationToken).ConfigureAwait(false);
            await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Audit log: structured, filterable.
            _logger.LogInformation(
                AuditEventId,
                "Attachment abort: {AttachmentId} by {UserId} (parent {ParentType}/{ParentId})",
                a.Id, request.RequestedBy, a.ParentType, a.ParentId);
        }
    }
}
