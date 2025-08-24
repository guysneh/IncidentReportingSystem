using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using IncidentReportingSystem.API.Middleware;
using IncidentReportingSystem.Application.Common.Exceptions;
using IncidentReportingSystem.IntegrationTests.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.Serialization;

namespace IncidentReportingSystem.IntegrationTests.Middleware
{
    public sealed class ExceptionHandlingTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly WebApplicationFactory<Program> _factory;

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

        private static Exception CreateExceptionRobust(Type t)
        {
            try { return (Exception)Activator.CreateInstance(t)!; } catch { /* ignore */ }

            var strCtor = t.GetConstructors()
                .FirstOrDefault(c =>
                {
                    var ps = c.GetParameters();
                    return ps.Length == 1 && ps[0].ParameterType == typeof(string);
                });
            if (strCtor != null) return (Exception)strCtor.Invoke(new object[] { "test" });

            var strInner = t.GetConstructors()
                .FirstOrDefault(c =>
                {
                    var ps = c.GetParameters();
                    return ps.Length == 2 && ps[0].ParameterType == typeof(string) &&
                           typeof(Exception).IsAssignableFrom(ps[1].ParameterType);
                });
            if (strInner != null) return (Exception)strInner.Invoke(new object[] { "test", new Exception("inner") });

            return (Exception)FormatterServices.GetUninitializedObject(t);
        }

        public static IEnumerable<object[]> KnownCases() => new[]
        {
            new object[] { typeof(InvalidCredentialsException), HttpStatusCode.Unauthorized, "Authentication failed" },
            new object[] { typeof(ForbiddenException),         HttpStatusCode.Forbidden,   "Forbidden" },
            new object[] { typeof(NotFoundException),          HttpStatusCode.NotFound,    "Not found" },
        };

        public ExceptionHandlingTests(CustomWebApplicationFactory baseFactory)
        {
            _factory = baseFactory.WithWebHostBuilder(builder =>
            {
                builder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/__test__/validation", () =>
                        {
                            var failures = new[]
                            {
                                new ValidationFailure("Email", "Invalid email format"),
                                new ValidationFailure("Password", "Too short")
                            };
                            throw new ValidationException(failures);
                        });

                        endpoints.MapGet("/__test__/invalid-credentials", () =>
                        {
                            throw new InvalidCredentialsException();
                        });

                        endpoints.MapGet("/__test__/email-exists", () =>
                        {
                            throw new EmailAlreadyExistsException("some email");
                        });

                        endpoints.MapGet("/__test__/account-locked", () =>
                        {
                            throw new AccountLockedException(DateTimeOffset.UtcNow.AddMinutes(10));
                        });

                        endpoints.MapGet("/__test__/argument", () =>
                        {
                            throw new ArgumentException("Bad argument value");
                        });

                        endpoints.MapGet("/__test__/unknown", () =>
                        {
                            throw new Exception("Boom");
                        });
                    });
                });
            });
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task ValidationException_Produces_400_With_ProblemDetails()
        {
            var client = _factory.CreateClient();
            var res = await client.GetAsync("/__test__/validation");
            res.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var problem = await res.Content.ReadFromJsonAsync<ProblemDetails>();
            problem!.Status.Should().Be((int)HttpStatusCode.BadRequest);
            problem.Title.Should().Be("Validation failed");
            problem.Extensions.Should().ContainKey("traceId");
            problem.Detail.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task InvalidCredentials_Produces_401_With_ProblemDetails()
        {
            var client = _factory.CreateClient();
            var res = await client.GetAsync("/__test__/invalid-credentials");
            res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var problem = await res.Content.ReadFromJsonAsync<ProblemDetails>();
            problem!.Status.Should().Be((int)HttpStatusCode.Unauthorized);
            problem.Title.Should().Be("Authentication failed");
            problem.Extensions.Should().ContainKey("traceId");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task EmailAlreadyExists_Produces_409_With_ProblemDetails()
        {
            var client = _factory.CreateClient();
            var res = await client.GetAsync("/__test__/email-exists");
            res.StatusCode.Should().Be(HttpStatusCode.Conflict);

            var problem = await res.Content.ReadFromJsonAsync<ProblemDetails>();
            problem!.Status.Should().Be((int)HttpStatusCode.Conflict);
            problem.Title.Should().Be("Email already exists");
            problem.Extensions.Should().ContainKey("traceId");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task AccountLocked_Produces_423_With_ProblemDetails()
        {
            var client = _factory.CreateClient();
            var res = await client.GetAsync("/__test__/account-locked");
            res.StatusCode.Should().Be((HttpStatusCode)423);

            var problem = await res.Content.ReadFromJsonAsync<ProblemDetails>();
            problem!.Status.Should().Be(423);
            problem.Title.Should().Be("Account locked");
            problem.Extensions.Should().ContainKey("traceId");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task ArgumentException_Produces_400_With_ProblemDetails()
        {
            var client = _factory.CreateClient();
            var res = await client.GetAsync("/__test__/argument");
            res.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var problem = await res.Content.ReadFromJsonAsync<ProblemDetails>();
            problem!.Status.Should().Be((int)HttpStatusCode.BadRequest);
            problem.Title.Should().Be("Invalid argument");
            problem.Extensions.Should().ContainKey("traceId");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task UnknownException_Produces_500_With_Generic_ProblemDetails()
        {
            var client = _factory.CreateClient();
            var res = await client.GetAsync("/__test__/unknown");
            res.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

            var problem = await res.Content.ReadFromJsonAsync<ProblemDetails>();
            problem!.Status.Should().Be((int)HttpStatusCode.InternalServerError);
            problem.Title.Should().Be("Unexpected error");
            problem.Extensions.Should().ContainKey("traceId");
        }

        [Theory(DisplayName = "Maps known exceptions to expected status/title")]
        [MemberData(nameof(KnownCases))]
        public async Task Maps_known_exceptions_to_expected_status(Type exType, HttpStatusCode expected, string title)
        {
            var ex = CreateExceptionRobust(exType);
            var client = MakeClient(ex);

            var res = await client.GetAsync("/");
            res.StatusCode.Should().Be(expected);

            var pd = await res.Content.ReadFromJsonAsync<ProblemDetails>();
            pd!.Title.Should().Be(title);
        }

        [Fact(DisplayName = "DbUpdateException (no Postgres inner) => 503 ServiceUnavailable")]
        public async Task DbUpdate_without_pg_inner_maps_to_503()
        {
            var ex = new Microsoft.EntityFrameworkCore.DbUpdateException("db down");
            var client = MakeClient(ex);

            var res = await client.GetAsync("/");
            res.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

            var pd = await res.Content.ReadFromJsonAsync<ProblemDetails>();
            pd!.Title.Should().Be("Database unavailable");
        }

        [Fact(DisplayName = "AttachmentAlreadyExistsException => 409 Conflict")]
        public async Task AttachmentAlreadyExists_409()
        {
            var ex = new AttachmentAlreadyExistsException("dup");
            var client = MakeClient(ex);

            var res = await client.GetAsync("/");
            res.StatusCode.Should().Be(HttpStatusCode.Conflict);

            var pd = await res.Content.ReadFromJsonAsync<ProblemDetails>();
            pd!.Title.Should().Be("Conflict");
        }
    }
}
