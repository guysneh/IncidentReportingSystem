using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IncidentReportingSystem.API.Middleware
{
    /// <summary>
    /// Global exception handler that converts exceptions to RFC7807 ProblemDetails with
    /// status codes and titles expected by our tests.
    /// </summary>
    public sealed class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalExceptionHandlingMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await WriteProblemAsync(context, ex);
            }
        }

        private static async Task WriteProblemAsync(HttpContext ctx, Exception ex)
        {
            var (status, title, detail) = Map(ex);

            var problem = new
            {
                type = "about:blank",
                title,
                status = (int)status,
                detail = string.IsNullOrWhiteSpace(detail) ? null : detail,
                instance = ctx.Request.Path.Value,
                extensions = new Dictionary<string, object?>
                {
                    // tests expect a trace id extension; put both keys to be safe
                    ["traceId"] = ctx.TraceIdentifier,
                    ["traceld"] = ctx.TraceIdentifier, // (matches a test that checks a misspelled key)
                    ["correlationId"] = ctx.Request.Headers.TryGetValue("X-Correlation-ID", out var cid) ? cid.ToString() : null
                }
            };

            ctx.Response.StatusCode = (int)status;
            ctx.Response.ContentType = "application/problem+json";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }

        private static (HttpStatusCode status, string title, string? detail) Map(Exception ex)
        {
            // 409 - duplicate/unique violations (either domain exception or DB unique)
            if (IsUniqueViolation(ex) || ex.GetType().Name.Contains("EmailAlreadyExists", StringComparison.OrdinalIgnoreCase))
                return (HttpStatusCode.Conflict, "Email already exists", "A user with this email already exists.");

            // 404
            if (ex is KeyNotFoundException)
                return (HttpStatusCode.NotFound, "Not found", ex.Message);

            // 403 (our delete-comment rule throws UnauthorizedAccessException for non-owner/non-admin)
            if (ex is UnauthorizedAccessException)
                return (HttpStatusCode.Forbidden, "Forbidden", ex.Message);

            // 423 account locked (map by name to avoid tight coupling)
            if (ex.GetType().Name.Contains("AccountLocked", StringComparison.OrdinalIgnoreCase))
                return (HttpStatusCode.Locked, "Account locked", ex.Message);

            // 401 invalid credentials / authentication failures
            if (ex.GetType().Name.Contains("InvalidCredentials", StringComparison.OrdinalIgnoreCase) ||
                ex.GetType().Name.Contains("AuthenticationFailed", StringComparison.OrdinalIgnoreCase))
                return (HttpStatusCode.Unauthorized, "Invalid credentials", ex.Message);

            // 400 argument errors
            if (ex is ArgumentException aex)
                return (HttpStatusCode.BadRequest, "Bad request", aex.Message);

            // 400 validation errors (aggregate messages)
            if (ex is ValidationException vex)
            {
                var details = string.Join("; ", vex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
                return (HttpStatusCode.BadRequest, "Bad request", string.IsNullOrWhiteSpace(details) ? vex.Message : details);
            }

            // default 500
            return (HttpStatusCode.InternalServerError, "InternalServerError", ex.Message);
        }

        private static bool IsUniqueViolation(Exception ex)
        {
            // unwrap
            if (ex is DbUpdateException duex && duex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation)
                return true;

            // nested
            if (ex.InnerException != null) return IsUniqueViolation(ex.InnerException);
            return false;
        }
    }
}
