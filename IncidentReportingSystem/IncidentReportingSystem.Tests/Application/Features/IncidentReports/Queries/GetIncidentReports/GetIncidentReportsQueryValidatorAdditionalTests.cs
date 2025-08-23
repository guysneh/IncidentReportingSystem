using System;
using FluentAssertions;
using Xunit;
using IncidentReportingSystem.Application.Features.IncidentReports.Queries.GetIncidentReports;

namespace IncidentReportingSystem.Tests.Application.Features.IncidentReports.Queries.GetIncidentReports
{
    public sealed class GetIncidentReportsQueryValidatorAdditionalTests
    {
        private readonly GetIncidentReportsQueryValidator _v = new();

        [Fact]
        [Trait("Category", "Unit")]
        public void Take_OutOfRange_Fails()
        {
            _v.Validate(new GetIncidentReportsQuery(Take: 0)).IsValid.Should().BeFalse();
            _v.Validate(new GetIncidentReportsQuery(Take: 201)).IsValid.Should().BeFalse();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Skip_Negative_Fails()
        {
            _v.Validate(new GetIncidentReportsQuery(Skip: -1)).IsValid.Should().BeFalse();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void ReportedAfter_Greater_Than_ReportedBefore_Fails()
        {
            var after = DateTime.UtcNow;
            var before = after.AddMinutes(-1);
            _v.Validate(new GetIncidentReportsQuery(ReportedAfter: after, ReportedBefore: before))
              .IsValid.Should().BeFalse();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Valid_Request_Passes()
        {
            _v.Validate(new GetIncidentReportsQuery(Skip: 0, Take: 50)).IsValid.Should().BeTrue();
        }
    }
}
