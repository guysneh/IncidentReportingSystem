using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace IncidentReportingSystem.UI.Core.Http
{
    /// <summary>
    /// DelegatingHandler that inspects non-success HTTP responses, attempts to parse
    /// RFC7807 ProblemDetails, and throws an HttpRequestException enriched with details.
    /// This centralizes error handling so UI code can show friendly messages consistently.
    /// </summary>
    public sealed class ProblemDetailsHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode) return response;

            string payload = string.Empty;
            try { payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false); }
            catch { /* ignore */ }

            string? title = null, detail = null, type = null;
            int? status = (int)response.StatusCode;

            // Try parse ProblemDetails (application/problem+json)
            try
            {
                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;

                if (root.TryGetProperty("title", out var t)) title = t.GetString();
                if (root.TryGetProperty("detail", out var d)) detail = d.GetString();
                if (root.TryGetProperty("type", out var y)) type = y.GetString();
                if (root.TryGetProperty("status", out var s) && s.TryGetInt32(out var st)) status = st;
            }
            catch
            {
                // Not a valid JSON or not a ProblemDetails shape; fall back to status text
            }

            var message = title ?? detail ?? $"{(int)response.StatusCode} {response.ReasonPhrase}";
            throw new HttpRequestException(
                message: $"{message} (type={type ?? "n/a"}, status={status}, url={request.RequestUri})",
                inner: null,
                statusCode: response.StatusCode);
        }
    }
}
