using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Application.Abstractions.Attachments
{
    /// <summary>
    /// Abstraction over the underlying storage (Azure Blob / Fake / Loopback in tests).
    /// Provides upload slot issuance, metadata probing, content streaming and deletion.
    /// </summary>
    public interface IAttachmentStorage
    {
        /// <summary>Create a storage-agnostic upload slot for a specific attachment.</summary>
        Task<CreateUploadSlotResult> CreateUploadSlotAsync(CreateUploadSlotRequest req, CancellationToken cancellationToken);

        /// <summary>
        /// Try to obtain server-observed properties (length/content-type/etag) of an already uploaded object.
        /// Returns <c>null</c> if the object does not exist.
        /// </summary>
        Task<UploadedBlobProps?> TryGetUploadedAsync(string storagePath, CancellationToken cancellationToken);

        /// <summary>Open a read-only stream to the stored object.</summary>
        Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken);

        /// <summary>Delete the stored object (idempotent).</summary>
        Task DeleteAsync(string storagePath, CancellationToken cancellationToken);

        /// <summary>
        /// Overwrite existing content with the provided stream and set the content type.
        /// Implementations should replace the object atomically from the caller perspective.
        /// </summary>
        Task OverwriteAsync(string storagePath, Stream content, string contentType, CancellationToken cancellationToken);
    }

    public sealed record CreateUploadSlotRequest(Guid AttachmentId, string FileName, string ContentType, string PathPrefix);

    public sealed record CreateUploadSlotResult
    {
        public string StoragePath { get; init; }
        public Uri UploadUrl { get; init; }
        public DateTimeOffset ExpiresAt { get; init; }
        public string Method { get; init; } = "PUT";
        public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();
        public CreateUploadSlotResult(string storagePath, Uri uploadUrl, DateTimeOffset expiresAt)
            => (StoragePath, UploadUrl, ExpiresAt) = (storagePath, uploadUrl, expiresAt);
        public CreateUploadSlotResult(string storagePath, Uri uploadUrl, DateTimeOffset expiresAt, string method, IReadOnlyDictionary<string, string> headers)
            : this(storagePath, uploadUrl, expiresAt)
        {
            Method = string.IsNullOrWhiteSpace(method) ? "PUT" : method;
            Headers = headers ?? new Dictionary<string, string>();
        }
    }

    /// <summary>Uploaded object properties as observed in storage.</summary>
    public sealed record UploadedBlobProps(long Length, string ContentType, string ETag);
}
