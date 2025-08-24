using System.Net;
using System.Text.Json;
using FluentValidation;
using IncidentReportingSystem.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IncidentReportingSystem.API.Middleware
{
    /// <summary>
    /// Maps exceptions to RFC7807 ProblemDetails (titles stable, details sanitized).
    /// </summary>
    public sealed class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = false
        };

        public GlobalExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var (status, title, detail, extras) = MapException(ex);

                var problem = new ProblemDetails
                {
                    Status = (int)status,
                    Title = title,
                    Detail = string.IsNullOrWhiteSpace(detail) ? BuildDetail(ex) : detail,
                    Instance = context.Request.Path
                };

                problem.Extensions["traceId"] = context.TraceIdentifier;

                if (extras is not null)
                {
                    foreach (var kv in extras)
                        problem.Extensions[kv.Key] = kv.Value;
                }

                _logger.LogError(ex, "Exception mapped to ProblemDetails {Status} {Title}", problem.Status, problem.Title);

                context.Response.Clear();
                context.Response.StatusCode = problem.Status ?? (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
            }
        }

        private static string BuildDetail(Exception ex) =>
            string.IsNullOrWhiteSpace(ex.Message) ? "An unexpected error occurred." : ex.Message;

        private static (HttpStatusCode status, string title, string? detail, IDictionary<string, object?>? extras)
            MapException(Exception ex)
        {
            var fullName = ex.GetType().FullName ?? ex.GetType().Name;
            if (fullName.StartsWith("Microsoft.IdentityModel.Tokens.SecurityToken", StringComparison.Ordinal))
                return (HttpStatusCode.Unauthorized, "Authentication failed", null, null);

            if (ex.GetType().Name is "InvalidCredentialsException")
                return (HttpStatusCode.Unauthorized, "Authentication failed", null, null);

            return ex switch
            {
                ValidationException fv when fv.Errors is not null => (
                    HttpStatusCode.BadRequest,
                    "Validation failed",
                    "One or more validation errors occurred.",
                    new Dictionary<string, object?>
                    {
                        ["errors"] = fv.Errors.Select(e => new
                        {
                            propertyName = e.PropertyName,
                            errorMessage = e.ErrorMessage
                        }).ToArray()
                    }),

                ArgumentException => (
                    HttpStatusCode.BadRequest,
                    "Invalid argument",
                    null,
                    null),

                System.Security.Authentication.AuthenticationException => (
                    HttpStatusCode.Unauthorized,
                    "Authentication failed",
                    null,
                    null),

                InvalidCredentialsException => (
                   HttpStatusCode.Unauthorized,
                   "Invalid credentials",
                   null,
                   null),

                EmailAlreadyExistsException => (
                   HttpStatusCode.Conflict,
                   "Email already exists",
                   null,
                   null),

                UnauthorizedAccessException => (
                    HttpStatusCode.Forbidden,
                    "Forbidden",
                    null,
                    null),

                ForbiddenException => (
                    HttpStatusCode.Forbidden,
                    "Forbidden",
                    null,
                    null),

                KeyNotFoundException => (
                    HttpStatusCode.NotFound,
                    "Not found",
                    null,
                    null),

                NotFoundException => (HttpStatusCode.NotFound, "Not found", null, null),

                DbUpdateException dbue when dbue.InnerException is PostgresException pg && pg.SqlState == "23505" => (
                    HttpStatusCode.Conflict,
                    "Conflict",
                    pg.Detail ?? "Duplicate key value violates unique constraint.",
                    new Dictionary<string, object?> { ["constraint"] = pg.ConstraintName }),

                Microsoft.EntityFrameworkCore.DbUpdateException  => (
                   HttpStatusCode.ServiceUnavailable,
                   "Database unavailable",
                   null,
                   null),

                var e2 when e2.GetType().Name.Contains("AlreadyExists", StringComparison.OrdinalIgnoreCase) => (
                    HttpStatusCode.Conflict,
                    "Email already exists",
                    e2.Message,
                    null),

                AccountLockedException => (
                    HttpStatusCode.Locked,
                    "Account locked",
                    null,
                    null),

                InvalidOperationException ioe => (
                    HttpStatusCode.Conflict,
                    "Conflict",
                    ioe.Message,
                    null),

                _ => (
                    HttpStatusCode.InternalServerError,
                    "Unexpected error",
                    null,
                    null)
            };
        }
    }
}
