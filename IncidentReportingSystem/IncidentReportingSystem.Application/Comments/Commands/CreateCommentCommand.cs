using IncidentReportingSystem.Application.Comments.DTOs;
using MediatR;

namespace IncidentReportingSystem.Application.Comments.Commands;

public sealed record CreateCommentCommand(
    Guid IncidentId,
    Guid AuthorId, 
    string Text
) : IRequest<CommentDto>;
