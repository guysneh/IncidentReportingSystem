using Microsoft.Extensions.Logging;

namespace IncidentReportingSystem.UI.Core.Dashboard;

public sealed class DashboardState
{
    private readonly IDashboardService _svc;
    private readonly ILogger<DashboardState> _log;

    public DashboardOverviewDto? Overview { get; private set; }
    public IReadOnlyList<TrendPoint> Trend { get; private set; } = Array.Empty<TrendPoint>();
    public DashboardQuery Query { get; private set; } =
        new(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-84)), // 12 שבועות אחרונים כברירת מחדל
            DateOnly.FromDateTime(DateTime.UtcNow),
            TimeResolution.Weekly);

    public bool IsLoading { get; private set; }
    public string? Error { get; private set; }
    public event Action? Changed;

    public DashboardState(IDashboardService svc, ILogger<DashboardState> log)
    {
        _svc = svc; _log = log;
    }

    public async Task ApplyAsync(DashboardQuery q, CancellationToken ct)
    {
        Query = q;
        await LoadAsync(ct);
    }

    public async Task LoadAsync(CancellationToken ct)
    {
        try
        {
            IsLoading = true; Error = null; Overview = null; Trend = Array.Empty<TrendPoint>(); Notify();

            var oTask = _svc.GetOverviewAsync(Query, ct);
            var tTask = _svc.GetTrendAsync(Query, ct);
            await Task.WhenAll(oTask, tTask);

            Overview = oTask.Result;
            Trend = tTask.Result;

            _log.LogInformation("Dashboard loaded q={@Q}: total={Total}, points={Pts}", Query, Overview.TotalIncidents, Trend.Count);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { Error = ex.Message; _log.LogError(ex, "Dashboard load failed"); }
        finally { IsLoading = false; Notify(); }
    }

    private void Notify() { try { Changed?.Invoke(); } catch { } }
}
