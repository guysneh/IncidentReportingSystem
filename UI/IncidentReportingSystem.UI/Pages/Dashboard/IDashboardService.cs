namespace IncidentReportingSystem.UI.Core.Dashboard;

public interface IDashboardService
{
    Task<DashboardOverviewDto> GetOverviewAsync(DashboardQuery q, CancellationToken ct);
    Task<IReadOnlyList<TrendPoint>> GetTrendAsync(DashboardQuery q, CancellationToken ct);
}
