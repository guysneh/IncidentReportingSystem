using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using IncidentReportingSystem.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace IncidentReportingSystem.API.Middleware;

/// <summary>
/// Centralized exception handling middleware producing RFC7807 Problem Details.
/// Maps domain/application exceptions to consistent HTTP status codes and payloads,
/// includes correlation/trace identifiers, and avoids leaking internals in production.
/// </summary>
public sealed class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _env = env ?? throw new ArgumentNullException(nameof(env));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex).ConfigureAwait(false);
        }
    }

    private sealed record ErrorShape(int Status, string Title, object? Detail, bool LogAsError);
    private sealed record FieldError(string Field, string Message);
    private sealed record MessageDto(string Message);
    private sealed record DevError(string Message, string? Exception);

    /// <summary>
    /// Maps known exceptions to HTTP status codes and ProblemDetails payloads.
    /// Unknown exceptions become 500 with a generic message (no internal details leaked).
    /// </summary>
    private async Task HandleExceptionAsync(HttpContext ctx, Exception ex)
    {
        // Enrich logging context
        using var _ = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["TraceId"] = ctx.TraceIdentifier,
            ["CorrelationId"] = ctx.Request.Headers.TryGetValue("X-Correlation-Id", out var cid) ? cid.ToString() : null
        });

        ErrorShape map = ex switch
        {
            // 400 – FluentValidation
            ValidationException vex => new ErrorShape(
                StatusCodes.Status400BadRequest,
                "Validation failed",
                vex.Errors.Select(e => new FieldError(e.PropertyName, e.ErrorMessage)).ToArray(),
                false),

            // 400 – Malformed JSON
            JsonException => new ErrorShape(
                StatusCodes.Status400BadRequest,
                "Invalid JSON payload",
                new[] { new MessageDto("Invalid JSON format") },
                false),

            // 401 – Auth failures
            InvalidCredentialsException ice => new ErrorShape(
                StatusCodes.Status401Unauthorized,
                "Authentication failed",
                new[] { new MessageDto(ice.Message) },
                false),

            UnauthorizedAccessException uae => new ErrorShape(
                StatusCodes.Status401Unauthorized,
                "Authentication failed",
                new[] { new MessageDto(uae.Message) },
                false),

            // 409 – Conflict (email exists)
            EmailAlreadyExistsException eaee => new ErrorShape(
                StatusCodes.Status409Conflict,
                "Email already exists",
                new[] { new MessageDto(eaee.Message) },
                false),

             // 423 – Account lockout (enable when you add the exception)
             AccountLockedException ale => new ErrorShape(
                 StatusCodes.Status423Locked,
                 "Account locked",
                 new[] { new MessageDto(ale.Message) },
                 false),

            // 400 – Bad arguments from app layer
            ArgumentException aex => new ErrorShape(
                StatusCodes.Status400BadRequest,
                "Invalid argument",
                new[] { new MessageDto(aex.Message) },
                false),

            // 500 – Fallback
            _ => new ErrorShape(
                StatusCodes.Status500InternalServerError,
                "Unexpected error",
                _env.IsDevelopment() ? new[] { new DevError(ex.Message, ex.GetType().FullName) } : null,
                true)
        };

        if (map.LogAsError) _logger.LogError(ex, "Unhandled exception");
        else _logger.LogWarning(ex, "Handled exception: {Title}", map.Title);

        ctx.Response.StatusCode = map.Status;
        ctx.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = map.Status,
            Title = map.Title,
            // Keep payload compact but structured – put serialized JSON in Detail
            Detail = map.Detail is null ? null : SerializeCompact(map.Detail),
            Instance = ctx.Request.Path
        };

        // Helpful metadata (non-standard extensions)
        problem.Extensions["traceId"] = ctx.TraceIdentifier;
        if (ctx.Request.Headers.TryGetValue("X-Correlation-Id", out var correlation))
            problem.Extensions["correlationId"] = correlation.ToString();

        await ctx.Response.WriteAsJsonAsync(problem).ConfigureAwait(false);
    }

    /// <summary>
    /// Serializes an object to a compact JSON string for the ProblemDetails.Detail field.
    /// Keeps payload small while preserving client-friendly structure.
    /// </summary>
    private static string SerializeCompact(object value) =>
        JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
}
