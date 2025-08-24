using System.Net;
using FluentAssertions;
using Xunit.Abstractions;

namespace IncidentReportingSystem.IntegrationTests.Utils;

public static class HttpResponseMessageShould
{
    public static async Task ShouldBeAsync(
        this HttpResponseMessage res,
        HttpStatusCode expected,
        ITestOutputHelper output,
        string? when = null)
    {
        var body = res.Content is null ? "" : await res.Content.ReadAsStringAsync();

        output.WriteLine("===== HTTP ASSERT =====");
        if (res.RequestMessage is not null)
        {
            output.WriteLine($"Request: {res.RequestMessage.Method} {res.RequestMessage.RequestUri}");
            foreach (var h in res.RequestMessage.Headers)
                output.WriteLine($"  ReqHdr: {h.Key} = {string.Join(",", h.Value)}");
        }
        output.WriteLine($"Status: {(int)res.StatusCode} {res.StatusCode}");
        foreach (var h in res.Headers)
            output.WriteLine($"  ResHdr: {h.Key} = {string.Join(",", h.Value)}");
        output.WriteLine($"Body: {(string.IsNullOrEmpty(body) ? "<empty>" : (body.Length <= 2000 ? body : body[..2000] + "…"))}");
        if (!string.IsNullOrWhiteSpace(when))
            output.WriteLine($"Context: {when}");
        output.WriteLine("=======================");

        res.StatusCode.Should().Be(expected);
    }
}
