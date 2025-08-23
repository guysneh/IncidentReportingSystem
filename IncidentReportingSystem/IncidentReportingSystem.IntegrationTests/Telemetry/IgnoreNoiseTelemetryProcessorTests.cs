using System.Collections.Generic;
using FluentAssertions;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Xunit;
using IncidentReportingSystem.Infrastructure.Telemetry;

namespace IncidentReportingSystem.IntegrationTests.Infrastructure.Telemetry
{
    [Trait("Category", "Integration")]
    public sealed class IgnoreNoiseTelemetryProcessorTests
    {
        private sealed class CapturingProcessor : ITelemetryProcessor
        {
            public readonly List<ITelemetry> Forwarded = new();
            public void Process(ITelemetry item) => Forwarded.Add(item);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void Drops_Root_404_And_Robots_404()
        {
            var sink = new CapturingProcessor();
            var proc = new IgnoreNoiseTelemetryProcessor(sink);

            var root404 = new RequestTelemetry { Url = new System.Uri("http://x/"), ResponseCode = "404" };
            proc.Process(root404);

            var robots404 = new RequestTelemetry { Url = new System.Uri("http://x/robots.txt"), ResponseCode = "404" };
            proc.Process(robots404);

            sink.Forwarded.Should().BeEmpty(); 
        }

        [Fact]
        [Trait("Category", "Integration")]
        public void Forwards_NonRequest_And_Non404_And_NonNoisyPath()
        {
            var sink = new CapturingProcessor();
            var proc = new IgnoreNoiseTelemetryProcessor(sink);

            proc.Process(new TraceTelemetry("hello"));
            proc.Process(new RequestTelemetry { Url = new System.Uri("http://x/"), ResponseCode = "200" });
            proc.Process(new RequestTelemetry { Url = new System.Uri("http://x/api/health"), ResponseCode = "404" });

            sink.Forwarded.Count.Should().Be(3);
        }
    }
}
