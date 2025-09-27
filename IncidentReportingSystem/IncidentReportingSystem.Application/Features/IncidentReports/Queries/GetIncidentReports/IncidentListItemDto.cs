using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentReportingSystem.Application.Features.IncidentReports.Queries.GetIncidentReports
{
    public sealed record IncidentListItemDto(
    Guid Id,
    string ReporterId,
    string ReporterDisplayName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReportedAt,
    string Category,
    string SystemAffected,
    string Severity,
    string Status
    );
}
