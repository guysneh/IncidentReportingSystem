using FluentValidation;
using IncidentReportingSystem.Application.Behaviors;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions; 
using System.Threading;
using System.Threading.Tasks;
using Xunit;

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
    public async Task ValidatorsExist_AndValidationPasses_CallsNext()
    {
        var validators = new IValidator<DummyCommand>[] { new DummyValidator() };
        var logger = NullLogger<ValidationBehavior<DummyCommand, int>>.Instance;  
        var behavior = new ValidationBehavior<DummyCommand, int>(validators, logger); 

        var nextCalled = false;
        RequestHandlerDelegate<int> next = () => { nextCalled = true; return Task.FromResult(123); };

        var result = await behavior.Handle(new DummyCommand("ok"), next, CancellationToken.None);

        Assert.True(nextCalled);
        Assert.Equal(123, result);
    }
}
