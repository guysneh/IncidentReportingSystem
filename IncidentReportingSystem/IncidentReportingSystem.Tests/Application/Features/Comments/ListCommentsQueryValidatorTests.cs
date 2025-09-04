using System;
using FluentAssertions;
using IncidentReportingSystem.Application.Features.Comments.Queries.ListComment;
using Xunit;

namespace IncidentReportingSystem.Tests.Application.Features.Comments
{
    public sealed class ListCommentsQueryValidatorTests
    {
        [Theory]
        [InlineData(0, 1)]
        [InlineData(0, 50)]
        [InlineData(10, 200)]
        public void Valid_Inputs_Should_Pass(int skip, int take)
        {
            var v = new ListCommentsQueryValidator();
            var res = v.Validate(new ListCommentsQuery(Guid.NewGuid(), skip, take));
            res.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(-1, 10)]  // negative skip
        [InlineData(0, 0)]    // take too small
        [InlineData(0, 201)]  // take too large
        public void Invalid_Inputs_Should_Fail(int skip, int take)
        {
            var v = new ListCommentsQueryValidator();
            var res = v.Validate(new ListCommentsQuery(Guid.NewGuid(), skip, take));
            res.IsValid.Should().BeFalse();
        }
    }
}
