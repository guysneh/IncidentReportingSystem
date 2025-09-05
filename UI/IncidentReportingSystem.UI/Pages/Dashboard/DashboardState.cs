using Microsoft.Extensions.Logging;

namespace IncidentReportingSystem.UI.Core.Dashboard;

/// <summary>
/// Holds current dashboard data and notifies subscribers on changes.
/// The Changed event is synchronous to avoid deadlocks/hangs in UI rendering.
/// </summary>
public sealed class DashboardState
{
    private readonly IDashboardService _svc;
    private readonly ILogger<DashboardState> _log;

    public DashboardOverviewDto? Overview { get; private set; }
    public bool IsLoading { get; private set; }
    public string? Error { get; private set; }

    // Synchronous event (no awaiting) to prevent UI hangs
    public event Action? Changed;

    public DashboardState(IDashboardService svc, ILogger<DashboardState> log)
    {
        _svc = svc;
        _log = log;
    }

    public async Task LoadAsync(CancellationToken ct)
    {
        try
        {
            IsLoading = true;
            Error = null;
            Notify();

            // Optional: tiny timeout guard so a hung HTTP call won't freeze the UI forever
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            // linkedCts.CancelAfter(TimeSpan.FromSeconds(15)); // enable if you want a hard cap

            var dto = await _svc.GetOverviewAsync(linkedCts.Token);
            Overview = dto;
            _log.LogInformation("Dashboard overview loaded: total={Total}, status={S}, severity={V}, category={C}",
                dto.TotalIncidents, dto.ByStatus.Count, dto.BySeverity.Count, dto.ByCategory.Count);
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            _log.LogError(ex, "Failed to load dashboard overview");
        }
        finally
        {
            IsLoading = false;
            Notify();
        }
    }

    private void Notify()
    {
        try { Changed?.Invoke(); }
        catch (Exception ex) { _log.LogError(ex, "DashboardState.Changed handler threw"); }
    }
}
