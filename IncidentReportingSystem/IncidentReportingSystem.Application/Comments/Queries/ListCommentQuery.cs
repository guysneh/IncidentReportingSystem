using IncidentReportingSystem.Domain.Entities;
using MediatR;

namespace IncidentReportingSystem.Application.Comments.Queries
{
    /// <summary>Query to list comments (newest first) with simple pagination.</summary>
    public sealed record ListCommentsQuery(Guid IncidentId, int Skip = 0, int Take = 50)
        : IRequest<IReadOnlyList<IncidentComment>>;
}