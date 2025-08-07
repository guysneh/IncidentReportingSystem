FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY IncidentReportingSystem.sln ./
COPY IncidentReportingSystem.API/IncidentReportingSystem.API.csproj IncidentReportingSystem.API/
COPY IncidentReportingSystem.Application/IncidentReportingSystem.Application.csproj IncidentReportingSystem.Application/
COPY IncidentReportingSystem.Domain/IncidentReportingSystem.Domain.csproj IncidentReportingSystem.Domain/
COPY IncidentReportingSystem.Infrastructure/IncidentReportingSystem.Infrastructure.csproj IncidentReportingSystem.Infrastructure/
COPY IncidentReportingSystem.Tests/IncidentReportingSystem.Tests.csproj IncidentReportingSystem.Tests/
COPY IncidentReportingSystem.IntegrationTests/IncidentReportingSystem.IntegrationTests.csproj IncidentReportingSystem.IntegrationTests/

RUN dotnet restore IncidentReportingSystem.sln

COPY . .
WORKDIR /src/IncidentReportingSystem.API
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

RUN rm -f appsettings.json

ENTRYPOINT ["dotnet", "IncidentReportingSystem.API.dll"]
