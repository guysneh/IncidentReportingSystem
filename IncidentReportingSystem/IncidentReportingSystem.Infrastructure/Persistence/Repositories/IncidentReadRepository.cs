using IncidentReportingSystem.Application.Abstractions;
using IncidentReportingSystem.Application.Abstractions.Persistence;
using IncidentReportingSystem.Application.Features.Statistics.Contracts;
using IncidentReportingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Globalization;

namespace IncidentReportingSystem.Infrastructure.Persistence.Repositories
{
    public sealed class IncidentReadRepository : IIncidentReadRepository
    {
        private readonly ApplicationDbContext _db;
        public IncidentReadRepository(ApplicationDbContext db) => _db = db;

        public async Task<IReadOnlyList<IncidentSeriesPoint>> GetIncidentSeriesAsync(
    DateTime from, DateTime to, TimeGranularity granularity, CancellationToken ct)
        {
            var times = await _db.IncidentReports
                .AsNoTracking()
                .Where(i => i.ReportedAt != null && i.ReportedAt >= from && i.ReportedAt < to)
                .Select(i => i.ReportedAt!.Value)
                .ToListAsync(ct);

            if (granularity == TimeGranularity.Day)
            {
                return times
                    .GroupBy(d => d.Date) 
                    .OrderBy(g => g.Key)
                    .Select(g => new IncidentSeriesPoint(g.Key.ToString("yyyy-MM-dd"), g.Count()))
                    .ToList();
            }
            else // Week (ISO)
            {
                return times
                    .GroupBy(d => new { Year = ISOWeek.GetYear(d), Week = ISOWeek.GetWeekOfYear(d) })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Week)
                    .Select(g => new IncidentSeriesPoint($"W{g.Key.Week:00}/{g.Key.Year}", g.Count()))
                    .ToList();
            }
        }
    }
}
