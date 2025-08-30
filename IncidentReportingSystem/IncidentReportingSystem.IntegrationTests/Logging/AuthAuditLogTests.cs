using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using IncidentReportingSystem.Application.Common.Logging;
using IncidentReportingSystem.IntegrationTests.Infrastructure.Logging;
using IncidentReportingSystem.IntegrationTests.Utils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IncidentReportingSystem.IntegrationTests.Logging;

public sealed class LoggingWebAppFactory : CustomWebApplicationFactory
{
    public TestLoggerProvider Provider { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureLogging(lb =>
        {
            lb.ClearProviders();
            lb.AddProvider(Provider);
        });
    }
}

public sealed class AuthAuditLogTests : IClassFixture<LoggingWebAppFactory>
{
    private readonly LoggingWebAppFactory _factory;
    public AuthAuditLogTests(LoggingWebAppFactory f) => _factory = f;

    [Fact(DisplayName = "Auth.Login emits audit log with EventId + tags")]
    public async Task Login_Emits_Audit_Log()
    {
        var client = _factory.CreateClient();

        var email = $"{Guid.NewGuid():N}@example.com";
        var register = await client.PostAsJsonAsync(RouteHelper.R(_factory, "Auth/register"),
            new { Email = email, Password = "P@ssw0rd!", Roles = new[] { "User" } });
        register.IsSuccessStatusCode.Should().BeTrue();

        var login = await client.PostAsJsonAsync(RouteHelper.R(_factory, "Auth/login"),
            new { Email = email, Password = "P@ssw0rd!" });
        login.IsSuccessStatusCode.Should().BeTrue();

        var rec = _factory.Provider.Records
            .FirstOrDefault(r => r.EventId.Id == AuditEvents.Auth.Login.Id);
        rec.Should().NotBeNull("Login should emit audit log");

        rec!.TryGetTags().Should().Be("auth,login");
        rec.State.Should().NotContain(kv => kv.Key.Contains("Password", StringComparison.OrdinalIgnoreCase));
    }
}
