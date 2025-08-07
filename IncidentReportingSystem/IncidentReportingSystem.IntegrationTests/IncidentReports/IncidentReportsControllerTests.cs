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

namespace IncidentReportingSystem.Tests.Integration;

public class IncidentReportsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public IncidentReportsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = AuthenticatedHttpClientFactory.CreateClientWithTokenAsync(factory).GetAwaiter().GetResult();
        CleanupDatabase().Wait();
        _jsonOptions = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    private async Task CleanupDatabase()
    {
        var connectionString = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION")
            ?? "Host=localhost;Port=5444;Database=testdb;Username=testuser;Password=testpassword";

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand("TRUNCATE TABLE \"IncidentReports\" RESTART IDENTITY CASCADE;", conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private CreateIncidentReportCommand GenerateValidCommand() =>
        new(
            description: "Test incident",
            location: "Berlin",
            reporterId: Guid.NewGuid(),
            category: IncidentCategory.Security,
            systemAffected: "Backend",
            severity: IncidentSeverity.High,
            reportedAt: DateTime.UtcNow
        );

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