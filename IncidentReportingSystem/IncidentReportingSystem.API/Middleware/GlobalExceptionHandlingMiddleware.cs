using FluentValidation;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using System.Text.Json;

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
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Middleware pipeline handler.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (ValidationException ex)
        {
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
            _logger.LogError(ex, "Invalid JSON payload.");

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
                        message = ex.Message
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
                error = "An unexpected error occurred",
                message = ex.Message
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(result)).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Attempts to extract the JSON path from a JsonException message.
    /// </summary>
    private static string? ExtractJsonPathFromMessage(string message)
    {
        var marker = "Path: ";
        var startIndex = message.IndexOf(marker);
        if (startIndex == -1) return null;

        var endIndex = message.IndexOf(".", startIndex);
        if (endIndex == -1) return message.Substring(startIndex + marker.Length).Trim();

        return message.Substring(startIndex + marker.Length, endIndex - startIndex - marker.Length).Trim();
    }
}
