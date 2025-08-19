using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using IncidentReportingSystem.Application.Common.Exceptions; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using IncidentReportingSystem.Domain.Exceptions;

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

            // 401 – only for login/auth flow explicit failures (not for business authorization)
            InvalidCredentialsException ice => new ErrorShape(
                StatusCodes.Status401Unauthorized,
                "Authentication failed",
                new[] { new MessageDto(ice.Message) },
                false),

            // 403 – business authorization (e.g., stranger tries to delete a comment)
            UnauthorizedAccessException uae => new ErrorShape(
                StatusCodes.Status403Forbidden,
                "Forbidden",
                new[] { new MessageDto(uae.Message) },
                false),

            // 404 – missing entities
            KeyNotFoundException kex => new ErrorShape(
                StatusCodes.Status404NotFound,
                "Not found",
                new[] { new MessageDto(kex.Message) },
                false),

            // 409 – database conflicts (unique violation, constraint)
            Microsoft.EntityFrameworkCore.DbUpdateException due when
                due.InnerException is Npgsql.PostgresException pgex &&
                (pgex.SqlState == "23505" /* unique_violation */ ||
                 pgex.SqlState == "23503" /* foreign_key_violation */ ||
                 pgex.SqlState == "23514" /* check_violation */)
                => new ErrorShape(
                    StatusCodes.Status409Conflict,
                    "Database update error",
                    new[] { new { pgex.ConstraintName, pgex.SqlState } },
                    false),

            // 500 – fallback
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

    private ErrorShape MapDbUpdate(DbUpdateException ex)
    {
        var status = StatusCodes.Status409Conflict;
        string title = "Database update error";

        if (ex.InnerException is PostgresException pg)
        {
           
            switch (pg.SqlState)
            {
                case PostgresErrorCodes.ForeignKeyViolation:
                    status = StatusCodes.Status404NotFound;
                    title = "Related entity not found (FK violation)";
                    break;

                case PostgresErrorCodes.UniqueViolation:
                    status = StatusCodes.Status409Conflict;
                    title = "Unique constraint violation";
                    break;

                case PostgresErrorCodes.NotNullViolation:
                    status = StatusCodes.Status400BadRequest;
                    title = "A required field was null";
                    break;

                case PostgresErrorCodes.StringDataRightTruncation:
                    status = StatusCodes.Status400BadRequest;
                    title = "A string field exceeded the allowed length";
                    break;
            }

            var detail = new[] { new { Constraint = pg.ConstraintName, SqlState = pg.SqlState } };
            return new ErrorShape(status, title, detail, LogAsError: false);
        }

        return new ErrorShape(status, title, new[] { new MessageDto(ex.Message) }, LogAsError: true);
    }

}
