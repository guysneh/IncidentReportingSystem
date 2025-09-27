using MediatR;

namespace IncidentReportingSystem.Application.Features.Comments.Commands.Create;

public sealed record CreateCommentCommand(
    Guid IncidentId,
    Guid AuthorId,
    string Text
) : IRequest<CommentDto>;
