using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace IncidentReportingSystem.IntegrationTests.Utils;

public sealed class LoggingAttachmentsWebApplicationFactory : AttachmentsWebApplicationFactory
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
