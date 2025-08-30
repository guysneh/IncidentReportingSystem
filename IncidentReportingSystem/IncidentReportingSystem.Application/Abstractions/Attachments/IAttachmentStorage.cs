using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Application.Abstractions.Attachments
{
    /// <summary>Abstraction over the underlying storage (Azure Blob / Fake / Loopback in tests).</summary>
    public interface IAttachmentStorage
    {
        Task<CreateUploadSlotResult> CreateUploadSlotAsync(CreateUploadSlotRequest req, CancellationToken cancellationToken);
        Task<UploadedBlobProps?> TryGetUploadedAsync(string storagePath, CancellationToken cancellationToken);
        Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken);
        Task DeleteAsync(string storagePath, CancellationToken cancellationToken);
    }

    /// <summary>Upload slot creation request (storage-agnostic).</summary>
    public sealed record CreateUploadSlotRequest(Guid AttachmentId, string FileName, string ContentType, string PathPrefix);

    /// <summary>
    /// Upload slot creation response.
    /// </summary>
    /// <param name="StoragePath">Opaque provider path/key (never expose publicly).</param>
    /// <param name="UploadUrl">Absolute URL for client upload.</param>
    /// <param name="ExpiresAt">UTC expiration timestamp for the upload URL.</param>
    /// <param name="Method">HTTP method to use (e.g., "PUT").</param>
    /// <param name="Headers">Required request headers (e.g., for Azure Blob: x-ms-blob-type).</param>
    public sealed record CreateUploadSlotResult
    {
        public string StoragePath { get; init; }
        public Uri UploadUrl { get; init; }
        public DateTimeOffset ExpiresAt { get; init; }

        // New fields used by the UI to issue the upload correctly.
        public string Method { get; init; } = "PUT";
        public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();

        public CreateUploadSlotResult(string storagePath, Uri uploadUrl, DateTimeOffset expiresAt)
            => (StoragePath, UploadUrl, ExpiresAt) = (storagePath, uploadUrl, expiresAt);

        public CreateUploadSlotResult(
            string storagePath,
            Uri uploadUrl,
            DateTimeOffset expiresAt,
            string method,
            IReadOnlyDictionary<string, string> headers)
            : this(storagePath, uploadUrl, expiresAt)
        {
            Method = string.IsNullOrWhiteSpace(method) ? "PUT" : method;
            Headers = headers ?? new Dictionary<string, string>();
        }
    }

    /// <summary>Uploaded object properties as observed in storage.</summary>
    public sealed record UploadedBlobProps(long Length, string ContentType, string ETag);
}
