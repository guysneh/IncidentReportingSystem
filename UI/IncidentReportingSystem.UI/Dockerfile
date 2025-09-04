# ---- Build image ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy csproj and restore
COPY IncidentReportingSystem.UI.csproj ./
RUN dotnet restore

# copy src and publish
COPY . .
RUN dotnet publish -c Release -o /app/publish

# ---- Runtime image ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IncidentReportingSystem.UI.dll"]
