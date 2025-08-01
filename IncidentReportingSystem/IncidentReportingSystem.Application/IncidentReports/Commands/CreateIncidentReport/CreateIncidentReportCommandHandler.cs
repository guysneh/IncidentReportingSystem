using System;
using System.Threading;
using System.Threading.Tasks;
using IncidentReportingSystem.Application.Incidents.Commands.CreateIncident;
using IncidentReportingSystem.Domain.Entities;
using IncidentReportingSystem.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IncidentReportingSystem.Application.IncidentReports.Commands.CreateIncidentReport;

public class CreateIncidentReportCommandHandler : IRequestHandler<CreateIncidentReportCommand, Guid>
{
    private readonly IIncidentReportRepository _repository;
    private readonly ILogger<CreateIncidentReportCommandHandler> _logger;

    public CreateIncidentReportCommandHandler(
        IIncidentReportRepository repository,
        ILogger<CreateIncidentReportCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public Task<Guid> Handle(CreateIncidentReportCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
