using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Infrastructure.Identity
{
    public interface IUserDirectory
    {
        /// <summary>
        /// Resolves display names for the given user ids.
        /// </summary>
        Task<IDictionary<Guid, string>> ResolveDisplayNamesAsync(
            IEnumerable<Guid> userIds,
            CancellationToken ct = default);
    }
}
