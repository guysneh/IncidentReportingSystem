using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Infrastructure.Identity
{
    public sealed class NullUserDirectory : IUserDirectory
    {
        public Task<IDictionary<Guid, string>> ResolveDisplayNamesAsync(
            IEnumerable<Guid> userIds,
            CancellationToken ct = default)
        {
            var map = new Dictionary<Guid, string>();
            if (userIds is not null)
            {
                foreach (var id in userIds.Distinct())
                    map[id] = id.ToString();
            }
            return Task.FromResult<IDictionary<Guid, string>>(map);
        }
    }
}
