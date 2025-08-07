FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY IncidentReportingSystem.API/IncidentReportingSystem.API.csproj IncidentReportingSystem.API/
COPY IncidentReportingSystem.Application/IncidentReportingSystem.Application.csproj IncidentReportingSystem.Application/
COPY IncidentReportingSystem.Domain/IncidentReportingSystem.Domain.csproj IncidentReportingSystem.Domain/
COPY IncidentReportingSystem.Infrastructure/IncidentReportingSystem.Infrastructure.csproj IncidentReportingSystem.Infrastructure/

WORKDIR /src/IncidentReportingSystem.API
RUN dotnet restore

WORKDIR /src
COPY IncidentReportingSystem.API/ IncidentReportingSystem.API/
COPY IncidentReportingSystem.Application/ IncidentReportingSystem.Application/
COPY IncidentReportingSystem.Domain/ IncidentReportingSystem.Domain/
COPY IncidentReportingSystem.Infrastructure/ IncidentReportingSystem.Infrastructure/

# Build & Publish
WORKDIR /src/IncidentReportingSystem.API
RUN dotnet publish -c Release -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

RUN rm -f appsettings.json
RUN rm -f appsettings.Development.json

ENTRYPOINT ["dotnet", "IncidentReportingSystem.API.dll"]
