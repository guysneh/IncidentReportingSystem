using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using IncidentReportingSystem.API.Middleware;
using IncidentReportingSystem.Application.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IncidentReportingSystem.Tests.API.Middleware;

[Trait("Category", "Unit")]
public sealed class GlobalExceptionHandlingMiddlewareTests
{
    private static async Task<(int status, string title)> InvokeAndReadAsync(Exception ex)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = "/test";
        ctx.Response.Body = new MemoryStream(); // ensure readable body

        var mw = new GlobalExceptionHandlingMiddleware(
            // next: always throw the provided exception
            _ => throw ex,
            NullLogger<GlobalExceptionHandlingMiddleware>.Instance);

        await mw.Invoke(ctx);

        ctx.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(ctx.Response.Body);
        var root = doc.RootElement;
        var title = root.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
        return (ctx.Response.StatusCode, title);
    }

    [Fact(DisplayName = "ValidationException -> 400")]
    public async Task ValidationException_400()
    {
        var ex = new ValidationException(new[] { new ValidationFailure("prop", "invalid") });
        var (status, title) = await InvokeAndReadAsync(ex);
        status.Should().Be((int)HttpStatusCode.BadRequest);
        title.Should().Be("Validation failed");
    }

    [Fact(DisplayName = "ArgumentException -> 400")]
    public async Task ArgumentException_400()
    {
        var (status, _) = await InvokeAndReadAsync(new ArgumentException("bad"));
        status.Should().Be((int)HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "AuthenticationException -> 401")]
    public async Task AuthenticationException_401()
    {
        var (status, title) = await InvokeAndReadAsync(new System.Security.Authentication.AuthenticationException("bad auth"));
        status.Should().Be((int)HttpStatusCode.Unauthorized);
        title.Should().Be("Authentication failed");
    }

    [Fact(DisplayName = "ForbiddenException -> 403")]
    public async Task ForbiddenException_403()
    {
        var (status, title) = await InvokeAndReadAsync(new ForbiddenException("nope"));
        status.Should().Be((int)HttpStatusCode.Forbidden);
        title.Should().Be("Forbidden");
    }

    [Fact(DisplayName = "NotFoundException -> 404")]
    public async Task NotFoundException_404()
    {
        var (status, title) = await InvokeAndReadAsync(new NotFoundException("entity", Guid.NewGuid().ToString()));
        status.Should().Be((int)HttpStatusCode.NotFound);
        title.Should().Be("Not found");
    }

    [Fact(DisplayName = "DbUpdateException (non-Postgres) -> 503")]
    public async Task DbUpdateException_503()
    {
        var (status, title) = await InvokeAndReadAsync(new DbUpdateException("db unavailable", innerException: null));
        status.Should().Be((int)HttpStatusCode.ServiceUnavailable);
        title.Should().Be("Database unavailable");
    }

    [Fact(DisplayName = "Unexpected -> 500")]
    public async Task Unexpected_500()
    {
        var (status, title) = await InvokeAndReadAsync(new Exception("boom"));
        status.Should().Be((int)HttpStatusCode.InternalServerError);
        title.Should().Be("Unexpected error");
    }
}
