using MediatR;

namespace IncidentReportingSystem.Application.Features.Comments.Commands.Delete
{
    /// <summary>Request to delete an existing comment.</summary>
    /// <param name="IncidentId">Target incident.</param>
    /// <param name="CommentId">Comment to delete.</param>
    /// <param name="RequestedBy">Requester identity (from API claims).</param>
    /// <param name="RequesterIsAdmin">Whether the requester has Admin role.</param>
    public sealed record DeleteCommentCommand(Guid IncidentId, Guid CommentId, Guid RequestedBy, bool RequesterIsAdmin) : IRequest;
}