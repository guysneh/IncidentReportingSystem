using IncidentReportingSystem.Application.Features.Comments.Commands.Create;

namespace IncidentReportingSystem.Tests.Application.Features.Comments
{
    /// <summary>Unit tests for CreateCommentCommandValidator.</summary>
    public sealed class CreateCommentCommandValidatorTests
    {
        [Fact]
        public void Valid_Command_Passes()
        {
            var v = new CreateCommentCommandValidator();
            var ok = v.Validate(new CreateCommentCommand(Guid.NewGuid(), Guid.NewGuid(), "ok"));
            Assert.True(ok.IsValid);
        }

        [Fact]
        public void Empty_Text_Fails()
        {
            var v = new CreateCommentCommandValidator();
            var r = v.Validate(new CreateCommentCommand(Guid.NewGuid(), Guid.NewGuid(), ""));
            Assert.False(r.IsValid);
        }

        [Fact]
        public void Too_Long_Text_Fails()
        {
            var v = new CreateCommentCommandValidator();
            var txt = new string('x', 2001);
            var r = v.Validate(new CreateCommentCommand(Guid.NewGuid(), Guid.NewGuid(), txt));
            Assert.False(r.IsValid);
        }
    }
}
