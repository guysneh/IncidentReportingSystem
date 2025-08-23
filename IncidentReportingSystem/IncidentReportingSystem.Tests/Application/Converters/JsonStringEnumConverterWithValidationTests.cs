using System;
using System.Text.Json;
using IncidentReportingSystem.API.Converters;
using IncidentReportingSystem.Domain.Enums;
using Xunit;

namespace IncidentReportingSystem.Tests.Converters
{
    public class JsonStringEnumConverterWithValidationTests
    {
        private class Wrapper
        {
            public IncidentStatus Status { get; set; }
        }

        private static JsonSerializerOptions Options()
        {
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            // Register factory that creates strict enum converters
            opts.Converters.Add(new EnumConverterFactory());
            return opts;
        }

        [Fact]
        public void Serialize_Enum_As_String()
        {
            var obj = new Wrapper { Status = IncidentStatus.Closed };
            var json = JsonSerializer.Serialize(obj, Options());
            Assert.Contains("\"Closed\"", json);
        }

        [Fact]
        public void Deserialize_Valid_String_Ignoring_Case()
        {
            var json = "{\"status\":\"inprogress\"}";
            var obj = JsonSerializer.Deserialize<Wrapper>(json, Options());
            Assert.Equal(IncidentStatus.InProgress, obj!.Status);
        }

        [Fact]
        public void Deserialize_Invalid_String_Throws()
        {
            var json = "{\"status\":\"not-a-real-status\"}";
            Assert.ThrowsAny<JsonException>(() => JsonSerializer.Deserialize<Wrapper>(json, Options()));
        }
    }
}
