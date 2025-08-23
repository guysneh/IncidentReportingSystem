using FluentValidation;
using FluentValidation.Results;
using IncidentReportingSystem.API.Middleware;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using IncidentReportingSystem.IntegrationTests.Utils;
using Microsoft.AspNetCore.Builder;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Net;
using IncidentReportingSystem.Application.Common.Exceptions;

namespace IncidentReportingSystem.IntegrationTests.Middleware
{
    public sealed class ExceptionHandlingTests
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly WebApplicationFactory<Program> _factory;

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
    }
}
