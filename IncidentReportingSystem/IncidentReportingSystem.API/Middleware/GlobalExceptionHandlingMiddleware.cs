using System.Net;
using System.Text.Json;
using FluentValidation;
using IncidentReportingSystem.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

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
        catch (InvalidCredentialsException ex)
        {
            await WriteProblem(context, (int)HttpStatusCode.Unauthorized, ex.Message);
        }
        catch (UnauthorizedAccessException ex) 
        {
            await WriteProblem(context, (int)HttpStatusCode.Unauthorized, ex.Message);
        }
        catch (Exception ex)
        {
            var (status, title) = ex switch
            {
                EmailAlreadyExistsException => (StatusCodes.Status409Conflict, "Email already exists"),
                ArgumentException => (StatusCodes.Status400BadRequest, "Invalid argument"),
                _ => (StatusCodes.Status500InternalServerError, "Unexpected error")
            };

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = ex.Message,
                Instance = context.Request.Path
            };

            await context.Response.WriteAsJsonAsync(problem).ConfigureAwait(false);
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

    private static Task WriteProblem(HttpContext ctx, int status, string detail)
    {
        ctx.Response.ContentType = "application/problem+json";
        ctx.Response.StatusCode = status;
        var pd = new ProblemDetails { Status = status, Title = "Authentication failed", Detail = detail };
        return ctx.Response.WriteAsJsonAsync(pd);
    }
}
