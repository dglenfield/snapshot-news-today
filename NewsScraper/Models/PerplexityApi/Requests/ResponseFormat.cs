using System.Text.Json.Serialization;

namespace NewsScraper.Models.PerplexityApi.Requests;

/// <summary>
/// Enables structured JSON output formatting.
/// </summary>
internal class ResponseFormat
{
    public string Type { get; init; } = "json_schema";
    public JsonSchema Json_Schema { get; init; }
}

internal class JsonSchema
{
    public string Name { get; set; } = "top_stories";
    public bool Strict { get; set; } = true;
    public required Schema Schema { get; set; }
}

internal class Schema
{
    public bool AdditionalProperties { get; set; }
    public string Type { get; init; }
    public int? MinItems { get; set; }
    public int? MaxItems { get; set; }
    public Dictionary<string, PropertyType>? Properties { get; set; }
    public string[]? Required { get; set; }

    [JsonPropertyName("top_stories")]
    public Schema[]? TopStories { get; set; }

    [JsonPropertyName("items")]
    public SchemaItem? Items { get; set; }

    internal Schema(string type)
    {
        Type = type;
    }
}

internal class SchemaItem
{
    public required string Type { get; set; }
    public string[]? Required { get; set; }
    public Dictionary<string, PropertyType>? Properties { get; set; }
    public bool AdditionalProperties { get; set; }
}

internal class PropertyType
{
    public required string Type { get; set; }
    public string? Description { get; set; }
}
