using System.Text.Json;
using System.Text.Json.Serialization;

namespace SnapshotNewsToday.Common.Serialization;

public static class JsonConfig
{
    public static string ToJson(object value, JsonSerializerOptions options, CustomJsonSerializerOptions customOptions) =>
        JsonSerializer.Serialize(value, Customize(options, customOptions));

    private static JsonSerializerOptions Customize(JsonSerializerOptions options,
        CustomJsonSerializerOptions customOptions)
    {
        options = new JsonSerializerOptions(options); // Clone to avoid mutating the original
        if (customOptions.HasFlag(CustomJsonSerializerOptions.IgnoreNull))
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        if (customOptions.HasFlag(CustomJsonSerializerOptions.WriteIndented))
            options.WriteIndented = true;

        return options;
    }
}
