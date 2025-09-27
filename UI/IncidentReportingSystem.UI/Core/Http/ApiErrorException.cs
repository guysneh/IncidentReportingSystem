using System.Text.Json;
using System.Text.Json.Serialization;

namespace IncidentReportingSystem.UI.Core.Http;

public sealed class ApiErrorException : Exception
{
    public int StatusCode { get; }
    public string? Title { get; }
    public string? Detail { get; }
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ApiErrorException(
        int statusCode,
        string? title,
        string? detail,
        IReadOnlyDictionary<string, string[]>? errors,
        string rawBody)
        : base(BuildMessage(statusCode, title, detail, errors, rawBody))
    {
        StatusCode = statusCode;
        Title = title;
        Detail = detail;
        Errors = errors ?? new Dictionary<string, string[]>();
    }

    private static string BuildMessage(
        int status, string? title, string? detail,
        IReadOnlyDictionary<string, string[]>? errors, string raw)
    {
        var parts = new List<string> { $"HTTP {status}" };
        if (!string.IsNullOrWhiteSpace(title)) parts.Add(title!);
        if (!string.IsNullOrWhiteSpace(detail)) parts.Add(detail!);
        if (errors is { Count: > 0 })
        {
            var flat = string.Join("; ", errors.Select(kv => $"{kv.Key}: {string.Join(", ", kv.Value)}"));
            parts.Add(flat);
        }
        if (parts.Count == 1 && !string.IsNullOrWhiteSpace(raw))
            parts.Add(raw);
        return string.Join(" – ", parts);
    }

    // Helper to parse ProblemDetails / ValidationProblemDetails
    public static async Task<ApiErrorException> FromResponseAsync(HttpResponseMessage resp, CancellationToken ct = default)
    {
        var status = (int)resp.StatusCode;
        var raw = await resp.Content.ReadAsStringAsync(ct);
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            string? title = root.TryGetProperty("title", out var t) ? t.GetString() : null;
            string? detail = root.TryGetProperty("detail", out var d) ? d.GetString() : null;

            IReadOnlyDictionary<string, string[]>? errors = null;
            if (root.TryGetProperty("errors", out var e) && e.ValueKind == JsonValueKind.Object)
            {
                var dict = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in e.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                        dict[prop.Name] = prop.Value.EnumerateArray().Select(x => x.GetString() ?? "").ToArray();
                }
                errors = dict;
            }
            return new ApiErrorException(status, title, detail, errors, raw);
        }
        catch
        {
            return new ApiErrorException(status, null, null, null, raw);
        }
    }
}
