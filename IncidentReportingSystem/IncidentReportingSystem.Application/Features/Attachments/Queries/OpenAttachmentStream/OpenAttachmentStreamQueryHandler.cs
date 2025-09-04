using IncidentReportingSystem.Application.Abstractions.Attachments;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Common.Errors;
using IncidentReportingSystem.Application.Common.Exceptions;
using MediatR;

namespace IncidentReportingSystem.Application.Features.Attachments.Queries.OpenAttachmentStream
{
    /// <summary>Handler that validates state and returns a readable stream from storage.</summary>
    public sealed class OpenAttachmentStreamQueryHandler
        : IRequestHandler<OpenAttachmentStreamQuery, OpenAttachmentStreamResponse>
    {
        private readonly IAttachmentRepository _repo;
        private readonly IAttachmentStorage _storage;

        public OpenAttachmentStreamQueryHandler(IAttachmentRepository repo, IAttachmentStorage storage)
        {
            _repo = repo;
            _storage = storage;
        }

        public async Task<OpenAttachmentStreamResponse> Handle(OpenAttachmentStreamQuery request, CancellationToken cancellationToken)
        {
            var a = await _repo.GetReadOnlyAsync(request.AttachmentId, cancellationToken).ConfigureAwait(false)
                ?? throw new NotFoundException(AttachmentErrors.AttachmentNotFound);

            if (a.Size is null)
                throw new InvalidOperationException(AttachmentErrors.AttachmentNotCompleted);

            // Get props (including provider-computed ETag) and the content stream
            var props = await _storage.TryGetUploadedAsync(a.StoragePath, cancellationToken).ConfigureAwait(false);
            if (props is null)
                throw new InvalidOperationException(AttachmentErrors.UploadedObjectMissing);

            var stream = await _storage.OpenReadAsync(a.StoragePath, cancellationToken).ConfigureAwait(false);

            return new OpenAttachmentStreamResponse(
                stream,
                a.ContentType,
                a.FileName,
                props.ETag 
            );
        }
    }
}
