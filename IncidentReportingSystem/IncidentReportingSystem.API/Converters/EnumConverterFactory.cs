using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;

namespace IncidentReportingSystem.API.Converters
{
    /// <summary>
    /// Factory that applies JsonStringEnumConverterWithValidation to all enums automatically.
    /// </summary>
    public class EnumConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsEnum;
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var converterType = typeof(JsonStringEnumConverterWithValidation<>).MakeGenericType(typeToConvert);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }
    }
}
