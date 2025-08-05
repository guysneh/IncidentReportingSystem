using Microsoft.EntityFrameworkCore.Migrations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IncidentReportingSystem.API.Converters
{
    /// <summary>
    /// Custom JSON converter that validates enum values and returns meaningful error messages.
    /// </summary>
    /// <typeparam name="T">Enum type.</typeparam>
    public class JsonStringEnumConverterWithValidation<T> : JsonConverter<T> where T : struct, Enum
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var input = reader.GetString();

            if (Enum.TryParse<T>(input, ignoreCase: true, out var result))
                return result;

            var allowedValues = string.Join(", ", Enum.GetNames(typeof(T)));
            throw new JsonException($"Invalid value '{input}' for enum type '{typeof(T).Name}'. Valid values are: {allowedValues}");
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(writer, nameof(writer));
            writer.WriteStringValue(value.ToString());
        }
    }
}
