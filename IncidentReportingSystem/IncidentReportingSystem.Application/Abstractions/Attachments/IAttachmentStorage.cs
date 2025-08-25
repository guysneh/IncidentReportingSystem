using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Application.Abstractions.Attachments
{
    /// <summary>Abstraction over the underlying storage (Azure Blob / Fake in tests).</summary>
    public interface IAttachmentStorage
    {
        Task<CreateUploadSlotResult> CreateUploadSlotAsync(CreateUploadSlotRequest req, CancellationToken cancellationToken);
        Task<UploadedBlobProps?> TryGetUploadedAsync(string storagePath, CancellationToken cancellationToken);
        Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken);
        Task DeleteAsync(string storagePath, CancellationToken cancellationToken);
    }

    /// <summary>Upload slot creation request (storage-agnostic).</summary>
    public sealed record CreateUploadSlotRequest(Guid AttachmentId, string FileName, string ContentType, string PathPrefix);

    /// <summary>Upload slot creation response.</summary>
    /// <param name="StoragePath">Opaque provider path/key.</param>
    /// <param name="UploadUrl">Absolute URI for client upload (SAS in production).</param>
    /// <param name="ExpiresAt">UTC expiration timestamp for the upload URL.</param>
    public sealed record CreateUploadSlotResult(string StoragePath, Uri UploadUrl, DateTimeOffset ExpiresAt);

    /// <summary>Uploaded object properties as observed in storage.</summary>
    public sealed record UploadedBlobProps(long Length, string ContentType, string ETag);
}
