using System;
using IncidentReportingSystem.API.Converters;
using IncidentReportingSystem.Domain.Enums;
using Xunit;

namespace IncidentReportingSystem.Tests.Converters
{
    public class EnumConverterFactoryTests
    {
        [Fact]
        public void CanConvert_ReturnsTrue_ForEnums()
        {
            var factory = new EnumConverterFactory();
            Assert.True(factory.CanConvert(typeof(IncidentStatus)));
            Assert.True(factory.CanConvert(typeof(IncidentSeverity)));
        }

        [Fact]
        public void CanConvert_ReturnsFalse_ForNonEnums()
        {
            var factory = new EnumConverterFactory();
            Assert.False(factory.CanConvert(typeof(string)));
            Assert.False(factory.CanConvert(typeof(int)));
        }
    }
}
