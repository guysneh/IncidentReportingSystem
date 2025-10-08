using MediatR;

namespace IncidentReportingSystem.Application.Features.Users.Queries.ResolveDisplayNames
{
    public sealed record ResolveDisplayNamesQuery(IEnumerable<string> Ids)
     : IRequest<Dictionary<string, string>>;
}
