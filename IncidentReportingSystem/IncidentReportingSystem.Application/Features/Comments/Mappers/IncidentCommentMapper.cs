using IncidentReportingSystem.Application.Features.Comments.Dtos;
using IncidentReportingSystem.Domain.Entities;

namespace IncidentReportingSystem.Application.Features.Comments.Mappers;

/// <summary>Mapping helpers for converting domain comments to DTOs.</summary>
public static class IncidentCommentMapper
{
    public static CommentDto ToDto(this IncidentComment c) => new CommentDto
    {
        Id = c.Id,
        IncidentId = c.IncidentId,
        UserId = c.UserId,
        Text = c.Text,
        CreatedAtUtc = c.CreatedAtUtc
    };
}
