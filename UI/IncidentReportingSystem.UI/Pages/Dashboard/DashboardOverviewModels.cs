namespace IncidentReportingSystem.UI.Core.Dashboard;

/// <summary>Unified model the Dashboard page binds to.</summary>
public sealed record DashboardOverviewDto(
    int TotalIncidents,
    IReadOnlyList<KeyCount> ByStatus,
    IReadOnlyList<KeyCount> BySeverity,
    IReadOnlyList<KeyCount> ByCategory);

/// <summary>Generic key->count pair for charts.</summary>
public sealed record KeyCount(string Key, int Count);

internal static class DashboardModelHelpers
{
    public static int GetCount(this IReadOnlyList<KeyCount> list, string key)
        => list.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase))?.Count ?? 0;
}
