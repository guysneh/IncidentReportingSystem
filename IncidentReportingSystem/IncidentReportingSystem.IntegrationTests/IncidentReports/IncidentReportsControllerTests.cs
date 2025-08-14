using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Npgsql;
using IncidentReportingSystem.Domain.Enums;
using IncidentReportingSystem.Application.IncidentReports.Commands.CreateIncidentReport;
using IncidentReportingSystem.Application.IncidentReports.DTOs;
using IncidentReportingSystem.IntegrationTests.Utils;
using Microsoft.Extensions.Configuration;

namespace IncidentReportingSystem.Tests.Integration;

public class IncidentReportsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public IncidentReportsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = AuthenticatedHttpClientFactory.CreateClientWithToken(factory);
        CleanupDatabase().Wait();
        _jsonOptions = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    private async Task CleanupDatabase()
    {
        var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.Test.json")
        .AddEnvironmentVariables()
        .Build();

        var connectionString = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION");

        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("Missing required environment variable: TEST_DB_CONNECTION");

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand("TRUNCATE TABLE \"IncidentReports\" RESTART IDENTITY CASCADE;", conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private CreateIncidentReportCommand GenerateValidCommand(
       string? description = null,
       IncidentSeverity? severity = null)
    {
        return new CreateIncidentReportCommand(
            description: description ?? "Test incident",
            location: "Berlin",
            reporterId: Guid.NewGuid(),
            category: IncidentCategory.Security,
            systemAffected: "Backend",
            severity: severity ?? IncidentSeverity.High,
            reportedAt: DateTime.UtcNow
        );
    }

    [Fact(DisplayName = "POST /incidentreports creates a report")]
    [Trait("Category", "Integration")]
    public async Task PostIncidentReport_ShouldCreateReport()
    {
        var command = GenerateValidCommand();

        var response = await _client.PostAsJsonAsync("/api/v1/incidentreports", command, _jsonOptions);
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, $"Response body: {body}");
    }

    [Fact(DisplayName = "GET /incidentreports/{id} returns the report")]
    [Trait("Category", "Integration")]
    public async Task GetIncidentReportById_ShouldReturnReport()
    {
        var command = GenerateValidCommand();

        var postResponse = await _client.PostAsJsonAsync("/api/v1/incidentreports", command, _jsonOptions);
        postResponse.IsSuccessStatusCode.Should().BeTrue($"POST failed. Status: {postResponse.StatusCode}, Body: {await postResponse.Content.ReadAsStringAsync()}");
        var created = await postResponse.Content.ReadFromJsonAsync<IncidentReportDto>(_jsonOptions);

        var getResponse = await _client.GetAsync($"/api/v1/incidentreports/{created.Id}");
        var getBody = await getResponse.Content.ReadAsStringAsync();
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK, $"GET body: {getBody}");
    }

    [Fact(DisplayName = "GET /incidentreports returns list")]
    [Trait("Category", "Integration")]
    public async Task GetIncidentReports_ShouldReturnList()
    {
        await PostIncidentReport_ShouldCreateReport();

        var response = await _client.GetAsync("/api/v1/incidentreports");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"Response body: {body}");
    }

    [Fact(DisplayName = "PUT /incidentreports/{id}/status updates status")]
    [Trait("Category", "Integration")]
    public async Task UpdateIncidentStatus_ShouldSucceed()
    {
        var command = GenerateValidCommand();

        var postResponse = await _client.PostAsJsonAsync("/api/v1/incidentreports", command, _jsonOptions);
        postResponse.IsSuccessStatusCode.Should().BeTrue($"POST failed. Status: {postResponse.StatusCode}, Body: {await postResponse.Content.ReadAsStringAsync()}");
        var created = await postResponse.Content.ReadFromJsonAsync<IncidentReportDto>(_jsonOptions);

        var updatePayload = JsonContent.Create(IncidentStatus.Closed.ToCamelCase(), options: _jsonOptions);
        var updateResponse = await _client.PutAsync($"/api/v1/incidentreports/{created.Id}/status", updatePayload);
        var updateBody = await updateResponse.Content.ReadAsStringAsync();
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent, $"Update body: {updateBody}");
    }

    [Fact(DisplayName = "POST /incidentreports fails with invalid payload")]
    [Trait("Category", "Integration")]
    public async Task PostIncidentReport_ShouldFail_WhenPayloadInvalid()
    {
        var command = GenerateValidCommand();
        command.Description = ""; // Invalid

        var response = await _client.PostAsJsonAsync("/api/v1/incidentreports", command, _jsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "GET /incidentreports/{id} returns 404 for non-existent ID")]
    [Trait("Category", "Integration")]
    public async Task GetIncidentReportById_ShouldReturnNotFound()
    {
        var nonExistentId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/v1/incidentreports/{nonExistentId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "PUT /incidentreports/{id}/status fails for invalid status")]
    [Trait("Category", "Integration")]
    public async Task UpdateIncidentStatus_ShouldFail_ForInvalidStatus()
    {
        var command = GenerateValidCommand();
        var postResponse = await _client.PostAsJsonAsync("/api/v1/incidentreports", command, _jsonOptions);
        postResponse.IsSuccessStatusCode.Should().BeTrue();
        var created = await postResponse.Content.ReadFromJsonAsync<IncidentReportDto>(_jsonOptions);

        var invalidStatusPayload = JsonContent.Create("invalidStatus", options: _jsonOptions);
        var updateResponse = await _client.PutAsync($"/api/v1/incidentreports/{created.Id}/status", invalidStatusPayload);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "PUT /incidentreports/{id}/status returns 404 for non-existent ID")]
    [Trait("Category", "Integration")]
    public async Task UpdateIncidentStatus_ShouldReturnNotFound_ForNonExistentId()
    {
        var nonExistentId = Guid.NewGuid();
        var updatePayload = JsonContent.Create(IncidentStatus.Closed.ToCamelCase(), options: _jsonOptions);

        var updateResponse = await _client.PutAsync($"/api/v1/incidentreports/{nonExistentId}/status", updatePayload);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "GET /incidentreports filters by category")]
    [Trait("Category", "Integration")]
    public async Task GetIncidentReports_ShouldSupportCategoryFilter()
    {
        var command1 = GenerateValidCommand();
        command1.Category = IncidentCategory.Security;

        var command2 = GenerateValidCommand();
        command2.Category = IncidentCategory.PowerOutage;

        await _client.PostAsJsonAsync("/api/v1/incidentreports", command1, _jsonOptions);
        await _client.PostAsJsonAsync("/api/v1/incidentreports", command2, _jsonOptions);

        var response = await _client.GetAsync("/api/v1/incidentreports?category=poweroutage");
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"GET response: {content}");
        content.Should().Contain("PowerOutage").And.NotContain("Security");
    }
    [Fact(DisplayName = "POST /incidentreports returns 400 for invalid payload")]
    public async Task PostIncidentReport_ShouldReturnBadRequest_ForInvalidPayload()
    {
        var command = new { }; // Invalid payload (missing required fields)
        var response = await _client.PostAsJsonAsync("/api/v1/incidentreports", command, _jsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "PUT /incidentreports/{id}/status returns 400 for invalid status")]
    public async Task UpdateIncidentStatus_ShouldReturnBadRequest_ForInvalidStatus()
    {
        var command = GenerateValidCommand();
        var postResponse = await _client.PostAsJsonAsync("/api/v1/incidentreports", command, _jsonOptions);
        var created = await postResponse.Content.ReadFromJsonAsync<IncidentReportDto>(_jsonOptions);

        var invalidStatus = new { }; // Invalid payload
        var response = await _client.PutAsJsonAsync($"/api/v1/incidentreports/{created.Id}/status", invalidStatus);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "PUT /incidentreports/{id}/status returns 404 for unknown ID")]
    public async Task UpdateIncidentStatus_ShouldReturnNotFound_ForUnknownId()
    {
        var unknownId = Guid.NewGuid();
        var response = await _client.PutAsJsonAsync($"/api/v1/incidentreports/{unknownId}/status", IncidentStatus.Closed.ToCamelCase(), _jsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "GET /incidentreports/{id} returns 404 for unknown ID")]
    public async Task GetIncidentReportById_ShouldReturnNotFound_ForUnknownId()
    {
        var unknownId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/v1/incidentreports/{unknownId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "GET /incidentreports supports filtering by status")]
    public async Task GetIncidentReports_ShouldSupportStatusFilter()
    {
        var command = GenerateValidCommand();
        await _client.PostAsJsonAsync("/api/v1/incidentreports", command, _jsonOptions);

        var response = await _client.GetAsync("/api/v1/incidentreports?status=Open");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"Response body: {body}");
    }

    [Fact(DisplayName = "GET /incidentreports supports filtering by severity")]
    public async Task GetIncidentReports_ShouldSupportSeverityFilter()
    {
        var command = GenerateValidCommand();
        command.Severity = IncidentSeverity.Medium;
        await _client.PostAsJsonAsync("/api/v1/incidentreports", command, _jsonOptions);

        var response = await _client.GetAsync("/api/v1/incidentreports?severity=medium");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"Response body: {body}");
    }

    [Fact(DisplayName = "GET /incidentreports supports searchText filter")]
    public async Task GetIncidentReports_ShouldSupportSearchText()
    {
        var command = GenerateValidCommand(description :"custom search term");
        await _client.PostAsJsonAsync("/api/v1/incidentreports", command, _jsonOptions);

        var response = await _client.GetAsync("/api/v1/incidentreports?searchText=custom");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"Response body: {body}");
    }
}
internal static class EnumExtensions
{
    public static string ToCamelCase(this string value)
    {
        if (string.IsNullOrEmpty(value) || char.IsLower(value[0]))
            return value;

        return char.ToLowerInvariant(value[0]) + value.Substring(1);
    }

    public static string ToCamelCase(this Enum value) => value.ToString().ToCamelCase();
}