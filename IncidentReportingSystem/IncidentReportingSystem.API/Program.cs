using FluentValidation;
using IncidentReportingSystem.Application.Common.Behaviors;
using IncidentReportingSystem.Application.IncidentReports.Commands.CreateIncidentReport;
using IncidentReportingSystem.Domain.Interfaces;
using IncidentReportingSystem.Infrastructure.IncidentReports.Repositories;
using IncidentReportingSystem.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateIncidentReportCommandHandler).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(CreateIncidentReportCommandValidator).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("IncidentReportsDb"));
builder.Services.AddScoped<IIncidentReportRepository, IncidentReportRepository>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
