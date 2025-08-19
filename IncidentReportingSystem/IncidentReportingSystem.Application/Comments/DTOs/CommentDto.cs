namespace IncidentReportingSystem.Application.Comments.DTOs
{
    /// <summary>Transport-friendly representation of a comment for API responses.</summary>
    public sealed class CommentDto
    {
        public Guid Id { get; init; }
        public Guid IncidentId { get; init; }
        public Guid UserId { get; init; }
        public string Text { get; init; } = string.Empty;
        public DateTime CreatedAtUtc { get; init; }
    }
}