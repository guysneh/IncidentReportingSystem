using IncidentReportingSystem.Domain.Entities;

namespace IncidentReportingSystem.Application.Features.Comments.Mappers
{
    public static class IncidentCommentMappers
    {
        public static CommentDto ToDto(this IncidentComment c) => new CommentDto
        {
            Id = c.Id,
            IncidentId = c.IncidentId,
            UserId = c.UserId,
            Text = c.Text ?? string.Empty,
            CreatedAtUtc = c.CreatedAtUtc
        };
    }
}
