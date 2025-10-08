// IncidentReportingSystem.Application/Features/Comments/Dtos/CommentDto.cs
public sealed class CommentDto
{
    public Guid Id { get; init; }
    public Guid IncidentId { get; init; }
    public Guid UserId { get; init; }
    public string? UserDisplayName { get; init; } // ← חדש
    public string Text { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
}
