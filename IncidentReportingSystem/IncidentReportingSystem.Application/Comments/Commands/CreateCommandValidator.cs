using FluentValidation;

namespace IncidentReportingSystem.Application.Comments.Commands
{
    public sealed class CreateCommentCommandValidator : AbstractValidator<CreateCommentCommand>
    {
        public CreateCommentCommandValidator()
        {
            RuleFor(x => x.IncidentId).NotEmpty();
            RuleFor(x => x.AuthorId).NotEmpty();
            RuleFor(x => x.Text)
                .NotEmpty()
                .Must(s => !string.IsNullOrWhiteSpace(s))
                .WithMessage("Text must not be empty or whitespace.")
                .MaximumLength(2000);
        }
    }
}