using IncidentReportingSystem.UI.Core.Statistics;
using IncidentReportingSystem.UI.Core.Http;

namespace IncidentReportingSystem.UI.Core.Dashboard;

public sealed class ApiDashboardService : IDashboardService
{
    private readonly IApiClient _api;
    public ApiDashboardService(IApiClient api) => _api = api;

    public async Task<DashboardOverviewDto> GetOverviewAsync(CancellationToken ct)
    {
        var wire = await _api.GetJsonAsync<IncidentStatisticsDto>("IncidentStatistics", ct)
                   ?? throw new InvalidOperationException("IncidentStatistics returned null payload.");

        static IReadOnlyList<KeyCount> Map(Dictionary<string, int> d) =>
            d.OrderByDescending(kv => kv.Value)
             .Select(kv => new KeyCount(kv.Key, kv.Value))
             .ToList();

        return new DashboardOverviewDto(
            TotalIncidents: wire.TotalIncidents,
            ByStatus: Map(wire.IncidentsByStatus ?? new Dictionary<string, int>()),
            BySeverity: Map(wire.IncidentsBySeverity ?? new Dictionary<string, int>()),
            ByCategory: Map(wire.IncidentsByCategory ?? new Dictionary<string, int>()));
    }
}
