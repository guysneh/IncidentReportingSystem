using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace IncidentReportingSystem.Application.Common.Logging;

/// <summary>
/// Logger helpers for consistent, filterable audit logs.
/// Exposes a simple "tags" scope that exporters map into structured fields.
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Begins a logging scope that adds a single string field named "tags"
    /// containing a comma-separated list of tags.
    /// </summary>
    /// <param name="logger">Logger to scope.</param>
    /// <param name="tags">Ordered list of tags to attach.</param>
    /// <returns>An <see cref="IDisposable"/> that ends the scope.</returns>
    public static IDisposable BeginAuditScope(this ILogger logger, params string[] tags)
    {
        var value = tags is { Length: > 0 } ? string.Join(',', tags) : string.Empty;
        // Using dictionary scope keeps exporters (Serilog, OTEL) happy.
        return logger.BeginScope(new Dictionary<string, object> { ["tags"] = value });
    }
}
