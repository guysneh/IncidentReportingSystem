using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;
using IncidentReportingSystem.Application.Features.IncidentReports.Commands.BulkUpdateIncidentStatus;

namespace IncidentReportingSystem.Tests.Application.IncidentReports.Commands.BulkUpdateIncidentStatus
{
    public sealed class BulkUpdateIncidentStatusValidatorAdditionalTests
    {
        private readonly BulkUpdateIncidentStatusValidator _v = new();

        [Fact]
        [Trait("Category", "Unit")]
        public void Missing_IdempotencyKey_Fails()
        {
            var cmd = new BulkUpdateIncidentStatusCommand("", new List<Guid> { Guid.NewGuid() }, IncidentReportingSystem.Domain.Enums.IncidentStatus.Open);
            _v.Validate(cmd).IsValid.Should().BeFalse();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Empty_Ids_Fails()
        {
            var cmd = new BulkUpdateIncidentStatusCommand("k1", new List<Guid>(), IncidentReportingSystem.Domain.Enums.IncidentStatus.Open);
            _v.Validate(cmd).IsValid.Should().BeFalse();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Valid_Request_Passes()
        {
            var cmd = new BulkUpdateIncidentStatusCommand("k1", new List<Guid> { Guid.NewGuid() }, IncidentReportingSystem.Domain.Enums.IncidentStatus.Closed);
            _v.Validate(cmd).IsValid.Should().BeTrue();
        }
    }
}
