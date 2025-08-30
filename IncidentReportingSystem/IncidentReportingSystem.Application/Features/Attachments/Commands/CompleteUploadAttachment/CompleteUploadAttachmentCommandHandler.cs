using FluentValidation;
using IncidentReportingSystem.Application.Abstractions.Attachments;
using IncidentReportingSystem.Application.Abstractions.Logging;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Common.Errors;
using IncidentReportingSystem.Application.Common.Exceptions;
using IncidentReportingSystem.Application.Common.Logging;
using IncidentReportingSystem.Application.Features.Attachments.Commands.CompleteUploadAttachment;
using IncidentReportingSystem.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Application.Features.Attachments.Commands
{
    /// <summary>
    /// Handler that verifies object presence/size/type and marks the entity as completed.
    /// Implements MediatR v12 non-generic IRequestHandler with Task return type.
    /// </summary>
    public sealed class CompleteUploadAttachmentCommandHandler : IRequestHandler<CompleteUploadAttachmentCommand>
    {
        private readonly IAttachmentRepository _repo;
        private readonly IAttachmentPolicy _policy;
        private readonly IAttachmentStorage _storage;
        private readonly IUnitOfWork _uow;
        private readonly IAttachmentAuditService _audit;
        public CompleteUploadAttachmentCommandHandler(
            IAttachmentRepository repo,
            IAttachmentPolicy policy,
            IAttachmentStorage storage,
            IUnitOfWork uow,
            IAttachmentAuditService audit)
        {
            _repo = repo;
            _policy = policy;
            _storage = storage;
            _uow = uow;
            _audit = audit;
        }

        /// <inheritdoc />
        public async Task Handle(CompleteUploadAttachmentCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repo.GetAsync(request.AttachmentId, cancellationToken).ConfigureAwait(false)
                ?? throw new NotFoundException(AttachmentErrors.AttachmentNotFound);

            if (entity.Status != AttachmentStatus.Pending)
                throw new InvalidOperationException(AttachmentErrors.AttachmentNotPending);

            var props = await _storage.TryGetUploadedAsync(entity.StoragePath, cancellationToken).ConfigureAwait(false);
            if (props is null)
                throw new InvalidOperationException(AttachmentErrors.UploadedObjectMissing);

            if (props.Length <= 0 || props.Length > _policy.MaxSizeBytes)
                throw new InvalidOperationException(AttachmentErrors.InvalidFileSize);

            if (!string.Equals(props.ContentType, entity.ContentType, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(AttachmentErrors.ContentTypeMismatch);

            entity.MarkCompleted(props.Length);
            await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Audit moved from controller to application layer
            _audit.AttachmentCompleted(entity.Id);
        }
    }
}
