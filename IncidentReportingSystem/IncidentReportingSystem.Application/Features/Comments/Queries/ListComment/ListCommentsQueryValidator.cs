using FluentValidation;

namespace IncidentReportingSystem.Application.Features.Comments.Queries.ListComment
{
    /// <summary>
    /// Validates paging arguments for listing incident comments.
    /// </summary>
    public sealed class ListCommentsQueryValidator : AbstractValidator<ListCommentsQuery>
    {
        public ListCommentsQueryValidator()
        {
            RuleFor(x => x.Skip).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Take).InclusiveBetween(1, 200);
        }
    }
}
