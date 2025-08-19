using IncidentReportingSystem.Application.Comments.DTOs;
using MediatR;

namespace IncidentReportingSystem.Application.Comments.Commands
{
    /// <summary>Request to create a new comment on an incident.</summary>
    /// <param name="IncidentId">Target incident.</param>
    /// <param name="AuthorId">Requester identity (from API claims).</param>
    /// <param name="Text">Comment content.</param>
    public sealed record CreateCommentCommand(Guid IncidentId, Guid AuthorId, string Text) : IRequest<CommentDto>;
}