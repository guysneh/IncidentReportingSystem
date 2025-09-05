namespace IncidentReportingSystem.UI.Core.Dashboard;

public interface IDashboardService
{
    Task<DashboardOverviewDto> GetOverviewAsync(CancellationToken ct);
}
