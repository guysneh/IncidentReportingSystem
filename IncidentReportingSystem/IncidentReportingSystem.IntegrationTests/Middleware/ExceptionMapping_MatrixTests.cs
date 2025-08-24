using FluentAssertions;
using IncidentReportingSystem.API.Middleware;
using IncidentReportingSystem.Application.Common.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Net.Http.Json;

namespace IncidentReportingSystem.IntegrationTests.Middleware;

[Trait("Category", "Integration")]
public sealed class ExceptionMapping_MatrixTests
{
    private static HttpClient MakeClient(Exception ex)
    {
        var app = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b =>
            {
                b.Configure(app =>
                {
                    app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
                    app.Run(_ => throw ex);
                });
            });
        return app.CreateClient();
    }

    public static IEnumerable<object?[]> Cases()
    {
        yield return new object?[] { new SecurityTokenExpiredException("expired"), HttpStatusCode.Unauthorized, "Authentication failed" };
        yield return new object?[] { new SecurityTokenInvalidSignatureException("bad sig"), HttpStatusCode.Unauthorized, "Authentication failed" };
        yield return new object?[] { new InvalidCredentialsException(), HttpStatusCode.Unauthorized, "Authentication failed" };

        yield return new object?[] { new ForbiddenException("Forbidden"), HttpStatusCode.Forbidden, "Forbidden" };
        yield return new object?[] { new UnauthorizedAccessException(), HttpStatusCode.Forbidden, "Forbidden" };

        yield return new object?[] { new NotFoundException("Not found"), HttpStatusCode.NotFound, "Not found" };
        yield return new object?[] { new KeyNotFoundException(), HttpStatusCode.NotFound, "Not found" };

        yield return new object?[] { new EmailAlreadyExistsException("Email already exists"), HttpStatusCode.Conflict, "Email already exists" };
        yield return new object?[] { new AttachmentAlreadyExistsException("dup"), HttpStatusCode.Conflict, "Conflict" };
        yield return new object?[] { new ArgumentException("bad arg"), HttpStatusCode.BadRequest, "Invalid argument" };

        // EF DbUpdateException without inner PostgresException → ServiceUnavailable branch
        yield return new object?[] { new Microsoft.EntityFrameworkCore.DbUpdateException("db down"), HttpStatusCode.ServiceUnavailable, "Database unavailable" };

        // Fallback branch → 500 Unexpected error
        yield return new object?[] { new Exception("boom"), HttpStatusCode.InternalServerError, "Unexpected error" };
    }

    [Theory(DisplayName = "MapException matrix covers all branches")]
    [MemberData(nameof(Cases))]
    public async Task Maps_known_exceptions_to_expected_status(Exception ex, HttpStatusCode expected, string expectedTitle)
    {
        var client = MakeClient(ex);

        var res = await client.GetAsync("/");
        res.StatusCode.Should().Be(expected);

        var pd = await res.Content.ReadFromJsonAsync<ProblemDetails>();
        pd!.Title.Should().Be(expectedTitle);
        pd.Extensions.Should().ContainKey("traceId");
    }
}
