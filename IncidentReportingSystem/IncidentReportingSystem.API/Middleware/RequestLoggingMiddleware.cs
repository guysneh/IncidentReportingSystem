namespace IncidentReportingSystem.API.Middleware;

/// <summary>
/// Middleware that logs incoming HTTP requests.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestLoggingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance.</param>
    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Logs the HTTP request details before passing to the next middleware.
    /// </summary>
    /// <param name="context">HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        var method = context.Request.Method;
        var path = context.Request.Path;
        var queryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : "";

        _logger.LogInformation("Incoming request: {Method} {Path}{Query}", method, path, queryString);

        if (context.Request.RouteValues is { Count: > 0 })
        {
            foreach (var (key, value) in context.Request.RouteValues)
            {
                _logger.LogDebug("Route value: {Key} = {Value}", key, value);
            }
        }

        await _next(context).ConfigureAwait(false);
    }
}
