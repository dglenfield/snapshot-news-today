using System.Text.Json;
using System.Text.Json.Serialization;

namespace SnapshotJob.Common.Serialization;

public class LowercaseJsonStringEnumConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsEnum;
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(LowercaseEnumConverter<>).MakeGenericType(typeToConvert);
        var converter = Activator.CreateInstance(converterType);
        return converter is null
            ? throw new InvalidOperationException($"Could not create converter for type {typeToConvert}.")
            : (JsonConverter)converter;
    }

    private class LowercaseEnumConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var enumText = reader.GetString() ?? throw new JsonException("Enum value cannot be null.");

            // Match case-insensitively
            if (Enum.TryParse(enumText, true, out T value))
                return value;

            throw new JsonException($"Unable to convert \"{enumText}\" to Enum \"{typeof(T)}\".");
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            // Write enum as lowercase
            writer.WriteStringValue(value.ToString().ToLowerInvariant());
        }
    }
}
