using MediatR;

namespace IncidentReportingSystem.Application.Features.Comments.Queries.GetById;

public sealed record GetCommentByIdQuery(Guid IncidentId, Guid CommentId) : IRequest<CommentDto>;
