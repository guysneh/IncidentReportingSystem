using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

using IncidentReportingSystem.API.Contracts.Authentication;
using IncidentReportingSystem.API.Controllers;
using IncidentReportingSystem.Application.Exceptions;
using IncidentReportingSystem.Application.Features.Users.Commands.LoginUser;
using IncidentReportingSystem.Application.Features.Users.Commands.RegisterUser;

namespace IncidentReportingSystem.Tests.API.Controllers
{
    [Trait("Category", "Unit")]
    public sealed class AuthControllerTests
    {
        private static AuthController NewController(Mock<IMediator>? m = null)
            => new AuthController((m ?? new Mock<IMediator>()).Object);

        [Fact]
        [Trait("Category", "Unit")]
        public async Task Login_Unauthorized_On_InvalidCredentials()
        {
            var m = new Mock<IMediator>(MockBehavior.Strict);
            m.Setup(x => x.Send(It.IsAny<LoginUserCommand>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new InvalidCredentialsException());

            var ctrl = NewController(m);
            var req = new LoginRequest { Email = "a@b.com", Password = "x" };

            var res = await ctrl.Login(req, CancellationToken.None);

            res.Result.Should().BeOfType<UnauthorizedObjectResult>();
            m.VerifyAll();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task Register_Conflict_On_EmailAlreadyExists()
        {
            var m = new Mock<IMediator>(MockBehavior.Strict);
            m.Setup(x => x.Send(It.IsAny<RegisterUserCommand>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new EmailAlreadyExistsException("a@b.com"));

            var ctrl = NewController(m);
            var cmd = new RegisterUserCommand("a@b.com", "P@ssw0rd!", new[] { "Operator" });

            var res = await ctrl.Register(cmd, CancellationToken.None);

            res.Should().BeOfType<ConflictObjectResult>();
            m.VerifyAll();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task Register_ServiceUnavailable_On_DbUpdateException()
        {
            var m = new Mock<IMediator>(MockBehavior.Strict);
            m.Setup(x => x.Send(It.IsAny<RegisterUserCommand>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new DbUpdateException("db err", (Exception?)null));

            var ctrl = NewController(m);
            var cmd = new RegisterUserCommand("a@b.com", "P@ssw0rd!", new[] { "Operator" });

            var res = await ctrl.Register(cmd, CancellationToken.None);

            res.Should().BeOfType<Microsoft.AspNetCore.Mvc.ConflictObjectResult>()
               .Which.StatusCode.Should().Be(503);
            m.VerifyAll();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public async Task Register_BadRequest_On_ArgumentOrValidation()
        {
            var m = new Mock<IMediator>(MockBehavior.Strict);
            m.Setup(x => x.Send(It.IsAny<RegisterUserCommand>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new ArgumentException("bad"));

            var ctrl = NewController(m);
            var cmd = new RegisterUserCommand("bad", "123", Array.Empty<string>());

            var res = await ctrl.Register(cmd, CancellationToken.None);

            res.Should().BeOfType<BadRequestObjectResult>();
            m.VerifyAll();
        }
    }
}
