using Serilog.Context;
using Serilog.Context;

namespace IncidentReportingSystem.API.Middleware;

/// <summary>
/// Middleware to ensure each request contains a correlation ID.
/// If not provided, a new GUID is generated. The ID is also added to the response headers and logging context.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string Header = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(Header, out var correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
            context.Request.Headers[Header] = correlationId;
        }

        context.Response.Headers[Header] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
