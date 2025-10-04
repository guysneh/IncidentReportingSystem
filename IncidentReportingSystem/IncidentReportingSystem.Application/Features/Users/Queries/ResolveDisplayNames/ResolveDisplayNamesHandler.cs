using IncidentReportingSystem.Application.Abstractions.Identity;
using MediatR;

namespace IncidentReportingSystem.Application.Features.Users.Queries.ResolveDisplayNames
{
    public sealed class ResolveDisplayNamesHandler
    : IRequestHandler<ResolveDisplayNamesQuery, Dictionary<string, string>>
    {
        private readonly IUserDirectory _directory;
        public ResolveDisplayNamesHandler(IUserDirectory directory) => _directory = directory;

        public async Task<Dictionary<string, string>> Handle(
            ResolveDisplayNamesQuery request, CancellationToken ct)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (request.Ids is null) return result;

            // שומרים את המפתח המקורי → Guid
            var guidMap = new Dictionary<Guid, string>();
            foreach (var raw in request.Ids.Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                if (Guid.TryParse(raw, out var g))
                {
                    if (!guidMap.ContainsKey(g))
                        guidMap[g] = raw;
                }
                else
                {
                    // fallback לא־Guid
                    result[raw] = raw;
                }
            }

            if (guidMap.Count == 0) return result;

            var resolved = await _directory.ResolveDisplayNamesAsync(guidMap.Keys, ct);

            // מרכיבים תשובה: שם תצוגה אם נמצא, אחרת fallback ל־ID המקורי
            foreach (var (g, originalKey) in guidMap)
            {
                if (resolved.TryGetValue(g, out var name) && !string.IsNullOrWhiteSpace(name))
                    result[originalKey] = name;
                else
                    result[originalKey] = originalKey;
            }

            return result;
        }
    }
}
