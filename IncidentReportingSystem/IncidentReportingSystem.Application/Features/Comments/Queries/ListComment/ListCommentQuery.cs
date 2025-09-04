using IncidentReportingSystem.Application.Common.Models;        
using IncidentReportingSystem.Application.Features.Comments.Dtos; 
using MediatR;

namespace IncidentReportingSystem.Application.Features.Comments.Queries.ListComment
{
    /// <summary>Query to list comments (newest first) with simple pagination.</summary>
    public sealed record ListCommentsQuery(Guid IncidentId, int Skip = 0, int Take = 50)
        : IRequest<PagedResult<CommentDto>>; 
}
