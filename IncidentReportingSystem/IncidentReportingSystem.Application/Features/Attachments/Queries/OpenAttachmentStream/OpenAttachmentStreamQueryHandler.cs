using IncidentReportingSystem.Application.Abstractions.Attachments;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Common.Errors;
using IncidentReportingSystem.Application.Common.Exceptions;
using MediatR;
using System.Security.Cryptography;
using System.Text;

namespace IncidentReportingSystem.Application.Features.Attachments.Queries.OpenAttachmentStream
{
    /// <summary>Handler that validates state and returns a readable stream from storage.</summary>
    public sealed class OpenAttachmentStreamQueryHandler
        : IRequestHandler<OpenAttachmentStreamQuery, OpenAttachmentStreamResponse>
    {
        private readonly IAttachmentsRepository _repo;
        private readonly IAttachmentStorage _storage;

        public OpenAttachmentStreamQueryHandler(IAttachmentsRepository repo, IAttachmentStorage storage)
        {
            _repo = repo;
            _storage = storage;
        }

        public async Task<OpenAttachmentStreamResponse> Handle(
            OpenAttachmentStreamQuery request, CancellationToken cancellationToken)
        {
            var a = await _repo.GetReadOnlyAsync(request.AttachmentId, cancellationToken).ConfigureAwait(false)
                ?? throw new NotFoundException(AttachmentErrors.AttachmentNotFound);

            if (a.Size is null)
                throw new InvalidOperationException(AttachmentErrors.AttachmentNotCompleted);

            // Provider props (may or may not have ETag/LastModified)
            var props = await _storage.TryGetUploadedAsync(a.StoragePath, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException(AttachmentErrors.UploadedObjectMissing);

            var stream = await _storage.OpenReadAsync(a.StoragePath, cancellationToken).ConfigureAwait(false);

            // Prefer provider ETag; otherwise build a deterministic fallback (no content read)
            var eTag = string.IsNullOrWhiteSpace(props.ETag)
                ? BuildFallbackEtag(a.Id, a.Size.Value, a.CompletedAt ?? a.CreatedAt)
                : props.ETag;

            var lastMod = a.CompletedAt ?? a.CreatedAt;

            return new OpenAttachmentStreamResponse(
                stream,
                a.ContentType,
                a.FileName,
                eTag,
                lastMod);
        }

        private static string BuildFallbackEtag(Guid id, long size, DateTimeOffset stamp)
        {
            var raw = $"{id:N}|{size}|{stamp.ToUnixTimeSeconds()}";
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            var b64 = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return $"W/\"{b64}\""; 
        }
    }
}
