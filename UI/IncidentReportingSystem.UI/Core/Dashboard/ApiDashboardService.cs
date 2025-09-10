using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using IncidentReportingSystem.UI.Core.Http;
using IncidentReportingSystem.UI.Core.Statistics;

namespace IncidentReportingSystem.UI.Core.Dashboard;

/// Service שמביא סטטיסטיקות וציר־זמן.
/// ציר־זמן: קודם מה-API הייעודי /stats/incidents/series, ואם אין — פולבאק מהרשימה.
/// Overview: תמיד מה-List כדי שטווח התאריכים יחול גם על Status/Severity/Category.
public sealed class ApiDashboardService : IDashboardService
{
    private readonly IApiClient _api;
    private const string TrendPath = "stats/incidents/series";

    public ApiDashboardService(IApiClient api) => _api = api;

    // ---------------- Overview (מוחל טווח תאריכים) ----------------
    public async Task<DashboardOverviewDto> GetOverviewAsync(DashboardQuery q, CancellationToken ct)
    {
        var total = 0;
        var byStatus = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var bySeverity = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var byCategory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        await foreach (var el in EnumerateIncidentsAsync(q, ct))
        {
            if (!TryGetDate(el, out var d)) continue;
            if (q.From.HasValue && d < q.From.Value) continue;
            if (q.To.HasValue && d > q.To.Value) continue;

            total++;

            Inc(byStatus, GetString(el, new[] { "status", "state" }) ?? "Unknown");
            Inc(bySeverity, GetString(el, new[] { "severity", "level", "priority" }) ?? "Unknown");
            Inc(byCategory, GetString(el, new[] { "category", "type", "incidentType", "classification" }) ?? "Unknown");
        }

        static IReadOnlyList<KeyCount> Map(IDictionary<string, int> d)
            => d.OrderByDescending(kv => kv.Value).Select(kv => new KeyCount(kv.Key, kv.Value)).ToList();

        return new DashboardOverviewDto(total, Map(byStatus), Map(bySeverity), Map(byCategory));
    }

    // ---------------- Trend ----------------
    public async Task<IReadOnlyList<TrendPoint>> GetTrendAsync(DashboardQuery q, CancellationToken ct)
    {
        // 1) נסה את ה-API הייעודי
        try
        {
            var url = $"{TrendPath}{BuildSeriesQuery(q)}";
            var json = await _api.GetJsonAsync<JsonElement>(url, ct);
            var fromServer = ParseSeries(json);
            if (fromServer.Count > 0)
            {
                // fill gaps לפי הטווח והרזולוציה
                var buckets = fromServer.ToDictionary(
                    p => q.Resolution == TimeResolution.Weekly ? WeekStart(p.Date) : p.Date,
                    p => p.Count);
                return FillGaps(buckets, q);
            }
        }
        catch { /* fallback */ }

        // 2) פולבאק מהרשימה
        var bucketsLocal = new Dictionary<DateOnly, int>();
        await foreach (var el in EnumerateIncidentsAsync(q, ct))
        {
            if (!TryGetDate(el, out var d)) continue;
            if (q.From.HasValue && d < q.From.Value) continue;
            if (q.To.HasValue && d > q.To.Value) continue;

            var key = q.Resolution == TimeResolution.Weekly ? WeekStart(d) : d;
            bucketsLocal[key] = bucketsLocal.TryGetValue(key, out var cur) ? cur + 1 : 1;
        }
        return FillGaps(bucketsLocal, q);
    }

    private static string BuildSeriesQuery(DashboardQuery q)
    {
        var parts = new List<string>();
        if (q.From is { } f) parts.Add($"from={f:yyyy-MM-dd}");
        if (q.To is { } t) parts.Add($"to={t:yyyy-MM-dd}");
        parts.Add($"period={(q.Resolution == TimeResolution.Weekly ? "weekly" : "daily")}");
        return parts.Count == 0 ? "" : "?" + string.Join("&", parts);
    }

    private static IReadOnlyList<TrendPoint> ParseSeries(JsonElement json)
    {
        var list = new List<TrendPoint>();

        if (json.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in json.EnumerateArray())
            {
                var ds = el.TryGetProperty("date", out var d) ? d.GetString() : null;
                var c = el.TryGetProperty("count", out var ce) ? SafeGetInt(ce) : 0;
                if (TryParseDate(ds, out var dd)) list.Add(new TrendPoint(dd, c));
            }
        }
        else if (json.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in json.EnumerateObject())
                if (TryParseDate(prop.Name, out var dd))
                    list.Add(new TrendPoint(dd, SafeGetInt(prop.Value)));
        }

        return list.OrderBy(p => p.Date).ToList();
    }

    // ---------------- Enumeration of incidents (GET, וריאציות עימוד/טווח) ----------------
    private async IAsyncEnumerable<JsonElement> EnumerateIncidentsAsync(DashboardQuery q, [EnumeratorCancellation] CancellationToken ct)
    {
        var endpoints = new[] { "IncidentReports", "Incidents", "Reports" };

        var rangePairs = new (string from, string to)[]
        {
            ("from","to"),
            ("startDate","endDate"),
            ("dateFrom","dateTo"),
            ("fromDate","toDate"),
            ("start","end")
        };

        var paging = new (string pageKey, string sizeKey, int startBase, bool isSkipTake)[]
        {
            ("page",       "pageSize",  1, false),
            ("pageNumber", "pageSize",  1, false),
            ("pageIndex",  "pageSize",  0, false),
            ("offset",     "limit",     0, true ),
            ("skip",       "take",      0, true ),
            ("",           "",          0, false), // ללא עימוד
        };

        const int pageSize = 1000;
        const int maxPages = 20;

        foreach (var ep in endpoints)
            foreach (var (fk, tk) in rangePairs)
                foreach (var (pageKey, sizeKey, startBase, isSkipTake) in paging)
                {
                    var any = false;

                    for (int page = startBase; page < startBase + maxPages; page++)
                    {
                        var parts = new List<string>();
                        if (q.From is { } f) parts.Add($"{fk}={f:yyyy-MM-dd}");
                        if (q.To is { } t) parts.Add($"{tk}={t:yyyy-MM-dd}");

                        if (!string.IsNullOrEmpty(pageKey))
                        {
                            if (isSkipTake)
                            {
                                var skip = page == 0 ? 0 : (page - startBase) * pageSize;
                                parts.Add($"{pageKey}={skip}");
                                parts.Add($"{sizeKey}={pageSize}");
                            }
                            else
                            {
                                parts.Add($"{pageKey}={page}");
                                parts.Add($"{sizeKey}={pageSize}");
                            }
                        }

                        var url = parts.Count == 0 ? ep : $"{ep}?{string.Join("&", parts)}";

                        JsonElement json;
                        try { json = await _api.GetJsonAsync<JsonElement>(url, ct); }
                        catch { break; }

                        int count = 0;
                        foreach (var item in ExtractArray(json)) { any = true; count++; yield return item; }

                        if (string.IsNullOrEmpty(pageKey)) break;     // אין עימוד
                        if (count < pageSize) break;                  // סוף עימוד
                    }

                    if (any) yield break; // וריאציה שעבדה – לא ממשיכים
                }
    }

    private static IEnumerable<JsonElement> ExtractArray(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var e in root.EnumerateArray()) yield return e;
            yield break;
        }
        if (root.ValueKind == JsonValueKind.Object)
        {
            string[] keys = { "items", "data", "results", "value", "values", "content", "records" };
            foreach (var k in keys)
                if (root.TryGetProperty(k, out var arr) && arr.ValueKind == JsonValueKind.Array)
                    foreach (var e in arr.EnumerateArray()) yield return e;
        }
    }

    // ---------------- Helpers ----------------
    private static bool TryGetDate(JsonElement el, out DateOnly date)
    {
        string[] keys = { "reportedAt", "reportedOn", "createdAt", "creationDate", "createdDate", "date", "timestamp", "occurredAt", "incidentDate", "reportedAtUtc", "eventDate", "time" };
        foreach (var k in keys)
        {
            if (!el.TryGetProperty(k, out var v)) continue;
            switch (v.ValueKind)
            {
                case JsonValueKind.String:
                    var s = v.GetString();
                    if (TryParseDate(s, out date)) return true;
                    if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt))
                    { date = DateOnly.FromDateTime(dt); return true; }
                    break;
                case JsonValueKind.Number:
                    if (v.TryGetInt64(out var n))
                    {
                        try
                        {
                            var dto = n > 3_000_000_000
                                ? DateTimeOffset.FromUnixTimeMilliseconds(n).UtcDateTime
                                : DateTimeOffset.FromUnixTimeSeconds(n).UtcDateTime;
                            date = DateOnly.FromDateTime(dto);
                            return true;
                        }
                        catch { }
                    }
                    break;
            }
        }
        date = default;
        return false;
    }

    private static string? GetString(JsonElement el, string[] keys)
    {
        foreach (var k in keys)
            if (el.TryGetProperty(k, out var v))
                return v.ValueKind switch
                {
                    JsonValueKind.String => v.GetString(),
                    JsonValueKind.Number => v.ToString(),
                    JsonValueKind.True => "True",
                    JsonValueKind.False => "False",
                    _ => null
                };
        return null;
    }

    private static void Inc(Dictionary<string, int> dict, string key)
        => dict[key] = dict.TryGetValue(key, out var v) ? v + 1 : 1;

    private static int SafeGetInt(JsonElement el) =>
        el.ValueKind switch
        {
            JsonValueKind.Number => el.TryGetInt32(out var i) ? i : 0,
            JsonValueKind.String => int.TryParse(el.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var i2) ? i2 : 0,
            _ => 0
        };

    private static bool TryParseDate(string? s, out DateOnly date) =>
        DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
        || (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt)
            ? (date = DateOnly.FromDateTime(dt)) != default
            : false);

    private static DateOnly WeekStart(DateOnly d)
    {
        var dow = (int)d.DayOfWeek; // Sunday=0
        var iso = dow == 0 ? 7 : dow;
        return d.AddDays(-(iso - 1)); // Monday
    }

    private static IReadOnlyList<TrendPoint> FillGaps(Dictionary<DateOnly, int> buckets, DashboardQuery q)
    {
        var result = new List<TrendPoint>();
        if (buckets.Count == 0) return result;

        var minKey = buckets.Keys.Min();
        var maxKey = buckets.Keys.Max();

        var start = q.From ?? minKey;
        var end = q.To ?? maxKey;

        if (q.Resolution == TimeResolution.Weekly)
        {
            for (var d = WeekStart(start); d <= WeekStart(end); d = d.AddDays(7))
                result.Add(new TrendPoint(d, buckets.TryGetValue(d, out var c) ? c : 0));
        }
        else
        {
            for (var d = start; d <= end; d = d.AddDays(1))
                result.Add(new TrendPoint(d, buckets.TryGetValue(d, out var c) ? c : 0));
        }
        return result;
    }
}
