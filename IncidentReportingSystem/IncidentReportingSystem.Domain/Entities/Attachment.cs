using IncidentReportingSystem.Domain.Enums;
using System;

namespace IncidentReportingSystem.Domain.Entities
{
    /// <summary>
    /// Represents a file attachment associated with an incident or a comment.
    /// The upload lifecycle is two-step: Start (slot creation) and Complete (server-side validation).
    /// </summary>
    public sealed class Attachment
    {
        /// <summary>Attachment identifier (stable for the lifetime of the entity).</summary>
        public Guid Id { get; private set; } = Guid.NewGuid();

        /// <summary>The type of parent entity this attachment belongs to.</summary>
        public AttachmentParentType ParentType { get; private set; }

        /// <summary>The identifier of the parent entity (incident or comment).</summary>
        public Guid ParentId { get; private set; }

        /// <summary>Original file name provided by the client.</summary>
        public string FileName { get; private set; } = null!;

        /// <summary>File MIME content type provided/validated at Start.</summary>
        public string ContentType { get; private set; } = null!;

        /// <summary>Final file size in bytes; <c>null</c> while pending.</summary>
        public long? Size { get; private set; }

        /// <summary>Lifecycle state of the attachment.</summary>
        public AttachmentStatus Status { get; private set; } = AttachmentStatus.Pending;

        /// <summary>
        /// Opaque storage path/key as returned by the storage provider (e.g., blob path).
        /// Never expose this path to clients.
        /// </summary>
        public string StoragePath { get; private set; } = null!;

        /// <summary>User identifier of the uploader (from authenticated context).</summary>
        public Guid UploadedBy { get; private set; }

        /// <summary>UTC timestamp of creation.</summary>
        public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

        /// <summary>UTC timestamp of completion; <c>null</c> while pending.</summary>
        public DateTimeOffset? CompletedAt { get; private set; }

        /// <summary>Whether a thumbnail representation exists (for images).</summary>
        public bool HasThumbnail { get; private set; }

        /// <summary>EF-only constructor.</summary>
        private Attachment() { }

        /// <summary>Creates a new attachment in a pending state.</summary>
        public Attachment(
            AttachmentParentType parentType,
            Guid parentId,
            string fileName,
            string contentType,
            string initialStoragePath,
            Guid uploadedBy)
        {
            ParentType = parentType;
            ParentId = parentId;
            FileName = fileName;
            ContentType = contentType;
            StoragePath = initialStoragePath;
            UploadedBy = uploadedBy;
        }

        /// <summary>Assigns the final storage path after the provider created the upload slot.</summary>
        /// <param name="storagePath">Provider-specific path (opaque).</param>
        public void AssignStoragePath(string storagePath)
        {
            StoragePath = storagePath ?? throw new ArgumentNullException(nameof(storagePath));
        }

        /// <summary>Marks the attachment as completed after server validation.</summary>
        /// <param name="size">Final content size in bytes.</param>
        /// <param name="hasThumbnail">Whether a thumbnail exists (applicable to images).</param>
        public void MarkCompleted(long size, bool hasThumbnail = false)
        {
            Size = size;
            Status = AttachmentStatus.Completed;
            CompletedAt = DateTimeOffset.UtcNow;
            HasThumbnail = hasThumbnail;
        }

        /// <summary>Blocks the attachment (e.g., malware). Blocked items cannot be downloaded.</summary>
        public void Block() => Status = AttachmentStatus.Blocked;
    }
}
