using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using IncidentReportingSystem.Application.Common.Behaviors;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace IncidentReportingSystem.Tests.Application.Common.Behaviors
{
    public class ValidationBehaviorTests
    {
        [Fact]
        [Trait("Category", "Unit")]
        public async Task Should_Invoke_ValidationBehavior()
        {
            // Arrange
            var validator = new DummyValidator();
            var loggerMock = new Mock<ILogger<ValidationBehavior<DummyRequest, TestResponse>>>();

            var behavior = new ValidationBehavior<DummyRequest, TestResponse>(
                new List<IValidator<DummyRequest>> { validator },
                loggerMock.Object
            );

            var request = new DummyRequest { Name = "Valid" };

            bool wasCalled = false;

            RequestHandlerDelegate<TestResponse> next = () =>
            {
                wasCalled = true;
                return Task.FromResult(new TestResponse());
            };

            // Act
            var response = await behavior.Handle(request, next, CancellationToken.None);

            // Assert
            response.ShouldNotBeNull();
            wasCalled.ShouldBeTrue();
        }
    }
    public class DummyRequest : IRequest<TestResponse>
    {
        public string Name { get; set; } = string.Empty;
    }

    public class TestResponse { }

    public class DummyValidator : AbstractValidator<DummyRequest>
    {
        public DummyValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}
