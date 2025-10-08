using IncidentReportingSystem.Application.Features.IncidentReports.Dtos;
using MediatR;

namespace IncidentReportingSystem.Application.Features.IncidentReports.Queries.GetIncidentReportDetails;

public sealed record GetIncidentReportDetailsQuery(Guid Id) : IRequest<IncidentReportDto>;
