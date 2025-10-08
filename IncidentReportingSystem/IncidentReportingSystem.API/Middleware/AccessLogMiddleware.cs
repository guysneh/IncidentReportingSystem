using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace IncidentReportingSystem.API.Middleware
{
    /// <summary>
    /// Structured HTTP access logs with sanitized headers/body + OTel TraceId, status & latency.
    /// </summary>
    public sealed class AccessLogMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AccessLogMiddleware> _logger;

        // redact these keys anywhere (headers + JSON body)
        private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "authorization","cookie","set-cookie","x-api-key","x-forwarded-for",
            "password","access_token","refresh_token","client_secret"
        };

        private const int MaxLoggedBytes = 4 * 1024;

        public AccessLogMiddleware(RequestDelegate next, ILogger<AccessLogMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var sw = ValueStopwatch.StartNew();

            // use OTel's Activity TraceId only (do NOT invent your own)
            var traceId = Activity.Current?.TraceId.ToString();

            var req = context.Request;
            var path = req.Path.ToString();
            var method = req.Method;
            var clientIp = context.Connection.RemoteIpAddress?.ToString();

            // sanitize headers
            var reqHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var h in req.Headers)
            {
                reqHeaders[h.Key] = SensitiveKeys.Contains(h.Key)
                    ? "***redacted***"
                    : Truncate(string.Join(",", h.Value), 256);
            }

            // capture limited sanitized request body for write methods
            string? requestBody = null;
            if (MayHaveBody(method))
            {
                req.EnableBuffering();
                using var reader = new StreamReader(req.Body, Encoding.UTF8, leaveOpen: true);
                var buf = new char[MaxLoggedBytes];
                var read = await reader.ReadBlockAsync(buf, 0, buf.Length);
                req.Body.Position = 0;
                requestBody = SanitizeBody(new string(buf, 0, read));
            }

            // capture response (status + latency); no need to log full body
            var original = context.Response.Body;
            await using var mem = new MemoryStream();
            context.Response.Body = mem;

            int status = 0;
            try
            {
                await _next(context);
                status = context.Response.StatusCode;
            }
            finally
            {
                mem.Position = 0;
                await mem.CopyToAsync(original);
                context.Response.Body = original;

                var latencyMs = sw.ElapsedMilliseconds;

                _logger.LogInformation(
                    "HTTP access {Method} {Path} -> {Status} in {Latency}ms | TraceId={TraceId} | ClientIP={ClientIP} | Headers={Headers} | Body={Body}",
                    method, path, status, latencyMs, traceId, clientIp, reqHeaders, requestBody);
            }
        }

        private static bool MayHaveBody(string method) =>
            method.Equals("POST", StringComparison.OrdinalIgnoreCase)
            || method.Equals("PUT", StringComparison.OrdinalIgnoreCase)
            || method.Equals("PATCH", StringComparison.OrdinalIgnoreCase);

        private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";

        private static string SanitizeBody(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return string.Empty;
            body = Truncate(body.Trim(), MaxLoggedBytes);

            // best-effort JSON redaction
            try
            {
                using var doc = JsonDocument.Parse(body);
                using var stream = new MemoryStream();
                using var writer = new Utf8JsonWriter(stream);
                RedactJson(doc.RootElement, writer);
                writer.Flush();
                return Encoding.UTF8.GetString(stream.ToArray());
            }
            catch
            {
                return body;
            }
        }

        private static void RedactJson(JsonElement el, Utf8JsonWriter w)
        {
            switch (el.ValueKind)
            {
                case JsonValueKind.Object:
                    w.WriteStartObject();
                    foreach (var p in el.EnumerateObject())
                    {
                        w.WritePropertyName(p.Name);
                        if (SensitiveKeys.Contains(p.Name))
                            w.WriteStringValue("***redacted***");
                        else
                            RedactJson(p.Value, w);
                    }
                    w.WriteEndObject();
                    break;
                case JsonValueKind.Array:
                    w.WriteStartArray();
                    foreach (var x in el.EnumerateArray()) RedactJson(x, w);
                    w.WriteEndArray();
                    break;
                default:
                    el.WriteTo(w);
                    break;
            }
        }

        private readonly struct ValueStopwatch
        {
            private static readonly double ToMs = 1000.0 / Stopwatch.Frequency;
            private readonly long _start;
            private ValueStopwatch(long start) => _start = start;
            public static ValueStopwatch StartNew() => new(Stopwatch.GetTimestamp());
            public long ElapsedMilliseconds => (long)((Stopwatch.GetTimestamp() - _start) * ToMs);
        }
    }
}
