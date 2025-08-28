using FluentValidation;
using IncidentReportingSystem.Application.Behaviors;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions; 
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IncidentReportingSystem.Tests.Application.Common.Behaviors;

public sealed class ValidationBehaviorWithValidatorsPassTests
{
    private sealed record DummyCommand(string Id) : IRequest<int>;

    private sealed class DummyValidator : AbstractValidator<DummyCommand>
    {
        public DummyValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }

    [Fact]
    public async Task When_ValidatorsExist_And_ValidationFails_Throws_ValidationException()
    {
        var validators = new IValidator<DummyCommand>[] { new DummyValidator() };
        var logger = NullLogger<ValidationBehavior<DummyCommand, int>>.Instance; 
        var behavior = new ValidationBehavior<DummyCommand, int>(validators, logger); 

        RequestHandlerDelegate<int> next = () => Task.FromResult(42);

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            behavior.Handle(new DummyCommand(""), next, CancellationToken.None));
    }
}
