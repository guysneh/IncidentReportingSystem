using System;
using FluentAssertions;
using Xunit;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Enums;

namespace IncidentReportingSystem.Tests.Domain
{
    public sealed class IncidentReportEntityTests
    {
        [Fact]
        [Trait("Category", "Unit")]
        public void Ctor_Sets_Defaults_And_Fields()
        {
            var now = DateTime.UtcNow.AddSeconds(-1);

            var ir = new IncidentReport(
                description: "API down",
                location: "Berlin",
                reporterId: Guid.NewGuid(),
                category: IncidentCategory.ITSystems,
                systemAffected: "Gateway",
                severity: IncidentSeverity.High,
                reportedAt: now
            );

            ir.Id.Should().NotBeEmpty();
            ir.Description.Should().Be("API down");
            ir.Location.Should().Be("Berlin");
            ir.Category.Should().Be(IncidentCategory.ITSystems);
            ir.SystemAffected.Should().Be("Gateway");
            ir.Severity.Should().Be(IncidentSeverity.High);
            ir.ReportedAt.Should().Be(now);
            ir.CreatedAt.Should().BeAfter(now);
            ir.Status.Should().Be(IncidentStatus.Open);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void UpdateStatus_Sets_Status_And_ModifiedAt()
        {
            var ir = new IncidentReport("x", "y", Guid.NewGuid(),
                IncidentCategory.Security, "Auth", IncidentSeverity.Low, DateTime.UtcNow);

            ir.SetModifiedAt(null);
            ir.UpdateStatus(IncidentStatus.Closed);

            ir.Status.Should().Be(IncidentStatus.Closed);
            ir.ModifiedAt.Should().NotBeNull();
        }
    }
}
