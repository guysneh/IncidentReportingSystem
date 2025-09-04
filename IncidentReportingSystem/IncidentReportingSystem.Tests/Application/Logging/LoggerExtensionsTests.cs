using IncidentReportingSystem.Application.Common.Logging;
using IncidentReportingSystem.Tests.Infrastructure.Logging;
using Microsoft.Extensions.Logging;
using Xunit;
using FluentAssertions;

namespace IncidentReportingSystem.Tests.Application.Logging;

public sealed class LoggerExtensionsTests
{
    [Fact]
    public void BeginAuditScope_writes_tags_scope()
    {
        var provider = new TestLoggerProvider();
        var logger = provider.CreateLogger("tests");

        using (logger.BeginAuditScope(AuditTags.Attachments, AuditTags.Complete))
        {
            logger.LogInformation(AuditEvents.Attachments.Complete, "done");
        }

        provider.Records.Should().HaveCount(1);
        var rec = provider.Records.Single();
        rec.EventId.Id.Should().Be(AuditEvents.Attachments.Complete.Id);
        rec.TryGetTags().Should().Be("attachments,complete");
    }
}
