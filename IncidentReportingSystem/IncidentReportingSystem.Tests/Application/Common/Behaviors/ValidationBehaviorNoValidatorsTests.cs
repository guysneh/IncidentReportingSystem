using System.Threading;
using System.Threading.Tasks;
using Xunit;
using MediatR;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;

namespace IncidentReportingSystem.Tests.Application.Common.Behaviors;

public class ValidationBehaviorNoValidatorsTests
{
    private record DummyRequest(string Value) : IRequest<string>;

    private class PassThroughHandler : IRequestHandler<DummyRequest, string>
    {
        public Task<string> Handle(DummyRequest request, CancellationToken cancellationToken)
            => Task.FromResult($"OK:{request.Value}");
    }

    [Fact]
    public async Task NoValidators_PassThrough_Success()
    {
        var validators = new List<FluentValidation.IValidator<DummyRequest>>(); 
        var behavior = new IncidentReportingSystem.Application.Behaviors.ValidationBehavior<DummyRequest, string>(validators,null);
        var handler = new PassThroughHandler();

        var result = await behavior.Handle(new DummyRequest("v"), () => handler.Handle(new DummyRequest("v"), CancellationToken.None), default);
        Assert.Equal("OK:v", result);
    }
}
