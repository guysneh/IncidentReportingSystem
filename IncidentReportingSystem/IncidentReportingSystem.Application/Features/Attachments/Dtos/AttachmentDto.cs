using IncidentReportingSystem.Domain.Enums;

namespace IncidentReportingSystem.Application.Features.Attachments.Dtos
{
    /// <summary>DTO for exposing attachment metadata via API.</summary>
    public sealed class AttachmentDto
    {
        public Guid Id { get; init; }
        public AttachmentParentType ParentType { get; init; }
        public Guid ParentId { get; init; }
        public string FileName { get; init; } = null!;
        public string ContentType { get; init; } = null!;
        public long? Size { get; init; }
        public AttachmentStatus Status { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? CompletedAt { get; init; }
        public bool HasThumbnail { get; init; }
        public bool CanDelete { get; init; }
        public bool CanDownload { get; init; }
    }
}
