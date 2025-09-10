using IncidentReportingSystem.UI.Core.Http;
using IncidentReportingSystem.UI.Core.Statistics;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace IncidentReportingSystem.UI.Core.Dashboard;

public sealed class ApiDashboardService : IDashboardService
{
    private readonly IApiClient _api;
    public ApiDashboardService(IApiClient api) => _api = api;

    public async Task<DashboardOverviewDto> GetOverviewAsync(DashboardQuery q, CancellationToken ct)
    {
        // אם אין טווח — ננסה את IncidentStatistics (כרגיל)
        if (q.From is null && q.To is null)
        {
            try
            {
                var wire = await _api.GetJsonAsync<IncidentStatisticsDto>("IncidentStatistics", ct);
                if (wire is not null)
                    return new DashboardOverviewDto(
                        wire.TotalIncidents,
                        MapDictionary(wire.IncidentsByStatus),
                        MapDictionary(wire.IncidentsBySeverity),
                        MapDictionary(wire.IncidentsByCategory));
            }
            catch { /* ניפול ל-list if needed */ }
        }

        // יש טווח, או שהקריאה היעודית נכשלה → בונים מה-LIST
        var built = await TryBuildOverviewFromListAsync(q, ct);
        return built ?? new DashboardOverviewDto(0, Array.Empty<KeyCount>(), Array.Empty<KeyCount>(), Array.Empty<KeyCount>());
    }

    private async Task<DashboardOverviewDto?> TryBuildOverviewFromListAsync(DashboardQuery q, CancellationToken ct)
    {
        var listEndpoints = new[] { "Incidents", "IncidentReports", "Reports" };
        var rangePairs = new (string from, string to)[] { ("from", "to"), ("startDate", "endDate") };

        foreach (var ep in listEndpoints)
            foreach (var (fromKey, toKey) in rangePairs)
            {
                var dto = await BuildOverviewFromListAsync(ep, fromKey, toKey, q, ct);
                if (dto is not null) return dto;
            }
        return null;
    }

    private async Task<DashboardOverviewDto?> BuildOverviewFromListAsync(
        string endpoint, string fromKey, string toKey, DashboardQuery q, CancellationToken ct)
    {
        const int pageSize = 1000;
        var page = 1;
        var total = 0;

        var byStatus = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var bySeverity = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var byCategory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        while (page <= 10)
        {
            var url = new StringBuilder(endpoint);
            url.Append($"?pageNumber={page}&pageSize={pageSize}");
            if (q.From is { } f) url.Append($"&{fromKey}={f:yyyy-MM-dd}");
            if (q.To is { } t) url.Append($"&{toKey}={t:yyyy-MM-dd}");

            JsonElement json;
            try { json = await _api.GetJsonAsync<JsonElement>(url.ToString(), ct); }
            catch { break; }

            var arr = ExtractArray(json).ToList();
            if (arr.Count == 0) break;

            foreach (var el in arr)
            {
                if (!TryGetDate(el, out var d)) continue;
                if (q.From.HasValue && d < q.From.Value) continue;
                if (q.To.HasValue && d > q.To.Value) continue;

                total++;

                var status = GetString(el, new[] { "status", "state" }) ?? "Unknown";
                var severity = GetString(el, new[] { "severity", "level", "priority" }) ?? "Unknown";
                var category = GetString(el, new[] { "category", "type", "incidentType" }) ?? "Unknown";

                Inc(byStatus, status);
                Inc(bySeverity, severity);
                Inc(byCategory, category);
            }

            if (arr.Count < pageSize) break;
            page++;
        }

        if (total == 0 && byStatus.Count == 0 && bySeverity.Count == 0 && byCategory.Count == 0)
            return null;

        static IReadOnlyList<KeyCount> Map(IDictionary<string, int> d)
            => d.OrderByDescending(kv => kv.Value).Select(kv => new KeyCount(kv.Key, kv.Value)).ToList();

        return new DashboardOverviewDto(total, Map(byStatus), Map(bySeverity), Map(byCategory));
    }

    private static void Inc(Dictionary<string, int> dict, string key)
        => dict[key] = dict.TryGetValue(key, out var v) ? v + 1 : 1;

    private static string? GetString(JsonElement el, string[] keys)
    {
        foreach (var k in keys)
            if (el.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String)
                return v.GetString();
        return null;
    }


    private static IReadOnlyList<KeyCount> MapDictionary<T>(IDictionary<string, T>? d)
        => (d ?? new Dictionary<string, T>())
            .Select(kv => new KeyCount(kv.Key, Convert.ToInt32(kv.Value)))
            .OrderByDescending(x => x.Count)
            .ToList();

    private static string BuildQuery(DashboardQuery q)
    {
        var p = new List<string>();
        if (q.From is { } f) p.Add($"from={f:yyyy-MM-dd}");
        if (q.To is { } t) p.Add($"to={t:yyyy-MM-dd}");
        p.Add($"period={(q.Resolution == TimeResolution.Weekly ? "weekly" : "daily")}");
        return p.Count == 0 ? string.Empty : "?" + string.Join("&", p);
    }

    private static readonly string[] TrendPaths = { "IncidentStatistics/Timeline", "IncidentStatistics/Daily" };

    public async Task<IReadOnlyList<TrendPoint>> GetTrendAsync(DashboardQuery q, CancellationToken ct)
    {
        // לא מנסים בכלל Endpoints של Timeline/TimeSeries כדי להימנע מ-405.
        // בונים ציר-זמן מתוך רשימת התקלות (GET) בצורה גמישה לשמות שדות שכיחים.

        // סדר עדיפויות של Endpoints לרשימות:
        var listEndpoints = new[] { "Incidents", "IncidentReports", "Reports" };

        foreach (var ep in listEndpoints)
        {
            // שני סטים נפוצים של שמות פרמטרים לטווח תאריכים:
            var rangePairs = new (string from, string to)[] {
            ("from","to"),
            ("startDate","endDate")
        };

            foreach (var (fromKey, toKey) in rangePairs)
            {
                var result = await TryBuildTrendFromListAsync(ep, fromKey, toKey, q, ct);
                if (result.Count > 0) return result; // הצליח – מחזירים
            }
        }

        // לא הצלחנו להוציא נתונים מהרשימות
        return Array.Empty<TrendPoint>();
    }

    private async Task<IReadOnlyList<TrendPoint>> TryBuildTrendFromListAsync(
        string endpoint,
        string fromKey,
        string toKey,
        DashboardQuery q,
        CancellationToken ct)
    {
        // עימוד בסיסי: pageNumber/pageSize (נוהג נפוץ); 1000 פר פריים כדי לא להציף
        const int pageSize = 1000;
        var page = 1;
        var buckets = new Dictionary<DateOnly, int>();

        while (page <= 10) // גבול בטוח כדי לא לרוץ ללא סוף
        {
            var url = new StringBuilder(endpoint);
            url.Append($"?pageNumber={page}&pageSize={pageSize}");

            if (q.From is { } f) url.Append($"&{fromKey}={f:yyyy-MM-dd}");
            if (q.To is { } t) url.Append($"&{toKey}={t:yyyy-MM-dd}");

            JsonElement json;
            try
            {
                json = await _api.GetJsonAsync<JsonElement>(url.ToString(), ct);
            }
            catch
            {
                // אם ה-endpoint הזה לא קיים/לא תומך בפרמטרים – מפסיקים לנסות את הצירוף הנוכחי
                break;
            }

            var arr = ExtractArray(json).ToList();
            if (arr.Count == 0)
                break; // אין עוד נתונים או מבנה לא נתמך

            foreach (var el in arr)
            {
                if (!TryGetDate(el, out var d))
                    continue;

                // סינון צד-לקוח (למקרה שהשרת לא סינן):
                if (q.From.HasValue && d < q.From.Value) continue;
                if (q.To.HasValue && d > q.To.Value) continue;

                var key = q.Resolution == TimeResolution.Weekly ? WeekStart(d) : d;
                buckets[key] = buckets.TryGetValue(key, out var cur) ? cur + 1 : 1;
            }

            // אם פחות מהעמוד המלא – אין טעם להמשיך
            if (arr.Count < pageSize) break;
            page++;
        }

        return buckets
            .OrderBy(kv => kv.Key)
            .Select(kv => new TrendPoint(kv.Key, kv.Value))
            .ToList();
    }

    // -------- Helpers (לוודא שקיימים בקובץ; אם כבר קיימים – אל תכפיל) --------

    private static IEnumerable<JsonElement> ExtractArray(JsonElement root)
    {
        // תומך במבנים: [ {...}, ... ]  או  { items:[...] }  או  { data:[...] }  או  { results:[...] }
        if (root.ValueKind == JsonValueKind.Array) return root.EnumerateArray();
        if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array) return items.EnumerateArray();
            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array) return data.EnumerateArray();
            if (root.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array) return results.EnumerateArray();
        }
        return Array.Empty<JsonElement>();
    }

    private static bool TryGetDate(JsonElement el, out DateOnly date)
    {
        // שמות שדה תאריך שכיחים
        var keys = new[] { "reportedAt", "createdAt", "occurredAt", "date", "timestamp" };

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
                    // ייתכן Unix ms
                    if (v.TryGetInt64(out var ticks))
                    {
                        try
                        {
                            var dto = DateTimeOffset.FromUnixTimeMilliseconds(ticks).UtcDateTime;
                            date = DateOnly.FromDateTime(dto);
                            return true;
                        }
                        catch { /* ignore */ }
                    }
                    break;
            }
        }

        date = default;
        return false;
    }

    private static DateOnly WeekStart(DateOnly d)
    {
        // ISO-8601: שבוע מתחיל ביום שני
        var dow = (int)d.DayOfWeek; // Sunday=0
        var iso = dow == 0 ? 7 : dow;
        return d.AddDays(-(iso - 1));
    }

    private static bool TryParseDate(string? s, out DateOnly date) =>
        DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
        || (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt)
            ? (date = DateOnly.FromDateTime(dt)) != default : false);
    private static IReadOnlyList<TrendPoint> BuildTrendFromList(JsonElement json, DashboardQuery q)
    {
        var buckets = new Dictionary<DateOnly, int>();

        foreach (var el in ExtractArray(json))
        {
            if (!TryGetDate(el, out var d)) continue;

            if (q.From.HasValue && d < q.From.Value) continue;
            if (q.To.HasValue && d > q.To.Value) continue;

            var key = q.Resolution == TimeResolution.Weekly ? WeekStart(d) : d;
            buckets[key] = buckets.TryGetValue(key, out var cur) ? cur + 1 : 1;
        }

        return FillGaps(buckets, q);
    }

    private static IReadOnlyList<TrendPoint> FillGaps(Dictionary<DateOnly, int> buckets, DashboardQuery q)
    {
        var result = new List<TrendPoint>();
        if (buckets.Count == 0) return result;

        // נקודות גבול
        var minKey = buckets.Keys.Min();
        var maxKey = buckets.Keys.Max();

        var start = q.From.HasValue
            ? (q.Resolution == TimeResolution.Weekly ? WeekStart(q.From.Value) : q.From.Value)
            : (q.Resolution == TimeResolution.Weekly ? WeekStart(minKey) : minKey);

        var end = q.To.HasValue
            ? (q.Resolution == TimeResolution.Weekly ? WeekStart(q.To.Value) : q.To.Value)
            : (q.Resolution == TimeResolution.Weekly ? WeekStart(maxKey) : maxKey);

        if (q.Resolution == TimeResolution.Weekly)
        {
            for (var d = start; d <= end; d = d.AddDays(7))
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
