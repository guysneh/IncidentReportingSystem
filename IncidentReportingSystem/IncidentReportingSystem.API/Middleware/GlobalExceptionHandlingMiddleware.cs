using System.Text.Json;
using FluentValidation;

namespace IncidentReportingSystem.API.Middleware;

/// <summary>
/// Middleware that handles all unhandled exceptions globally and returns structured JSON errors.
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Middleware pipeline handler.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation exception occurred.");

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";

            var result = new
            {
                error = "Validation failed",
                details = ex.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(result)).ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON payload received.");

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";

            var path = ExtractJsonPathFromMessage(ex.Message);

            var result = new
            {
                error = "Invalid JSON payload",
                details = new[]
                {
                    new
                    {
                        path = path ?? "unknown",
                        message = "Invalid JSON format"
                    }
                }
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(result)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred.");

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var result = new
            {
                error = "An unexpected error occurred"
                // intentionally omitting 'message = ex.Message' to avoid information disclosure
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(result)).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Attempts to extract the JSON path from a JsonException message.
    /// </summary>
    private static string? ExtractJsonPathFromMessage(string message)
    {
        const string marker = "Path: ";
        var startIndex = message.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (startIndex == -1) return null;

        var endIndex = message.IndexOf('.', startIndex);
        if (endIndex == -1)
            return message.Substring(startIndex + marker.Length).Trim();

        return message.Substring(startIndex + marker.Length, endIndex - startIndex - marker.Length).Trim();
    }
}
