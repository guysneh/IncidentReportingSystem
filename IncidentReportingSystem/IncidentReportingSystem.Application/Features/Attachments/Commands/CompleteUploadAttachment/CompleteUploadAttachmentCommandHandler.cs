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
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Application.Features.Attachments.Commands
{
    /// <summary>
    /// Handler that verifies object presence/size/type and marks the entity as completed.
    /// When configured to do so, it sanitizes image files (EXIF/ICC removal, orientation normalization)
    /// prior to completion and uses the sanitized size for the entity.
    /// </summary>
    public sealed class CompleteUploadAttachmentCommandHandler : IRequestHandler<CompleteUploadAttachmentCommand>
    {
        private readonly IAttachmentRepository _repo;
        private readonly IAttachmentPolicy _policy;
        private readonly IAttachmentStorage _storage;
        private readonly IUnitOfWork _uow;
        private readonly IAttachmentAuditService _audit;
        private readonly AttachmentOptions _options;
        private readonly IImageSanitizer _imageSanitizer;

        public CompleteUploadAttachmentCommandHandler(
            IAttachmentRepository repo,
            IAttachmentPolicy policy,
            IAttachmentStorage storage,
            IUnitOfWork uow,
            IAttachmentAuditService audit,
            IOptions<AttachmentOptions> options,
            IImageSanitizer imageSanitizer)
        {
            _repo = repo;
            _policy = policy;
            _storage = storage;
            _uow = uow;
            _audit = audit;
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _imageSanitizer = imageSanitizer ?? throw new ArgumentNullException(nameof(imageSanitizer));
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

            long finalLength = props.Length;

            // --- Optional image sanitization ---
            if (_options.SanitizeImages && props.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                var (changed, newLen, newCt) = await _imageSanitizer.TrySanitizeAsync(
                    entity.StoragePath, props.ContentType, cancellationToken).ConfigureAwait(false);

                if (changed && newLen > 0)
                {
                    finalLength = newLen;
                    // keep entity.ContentType as originally validated; do not mutate public contract here.
                }
            }

            entity.MarkCompleted(finalLength);
            await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _audit.AttachmentCompleted(entity.Id);
        }
    }
}
