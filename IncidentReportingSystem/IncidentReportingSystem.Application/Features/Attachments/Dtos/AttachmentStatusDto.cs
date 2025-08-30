namespace IncidentReportingSystem.Application.Features.Attachments.Dtos
{
    /// <summary>
    /// Represents current status information of an attachment,
    /// including lifecycle status, size, and storage existence.
    /// Status is returned as a string (e.g., "Pending", "Completed").
    /// </summary>
    public sealed class AttachmentStatusDto
    {
        public string Status { get; init; } = null!;
        public long? Size { get; init; }
        public bool ExistsInStorage { get; init; }
        public string? ContentType { get; init; }
    }
}
