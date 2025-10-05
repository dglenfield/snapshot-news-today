using System.Text.Json.Serialization;

namespace NewsScraper.Models.PerplexityApi.Requests.CurateArticles;

internal class ResponseFormat
{
    public string Type { get; init; } = "json_schema";
    public JsonSchema Json_Schema { get; init; } = new();
}

internal class JsonSchema : JsonSchemaBase
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";

    [JsonPropertyName("required")]
    public string[] Required { get; set; } = ["top_stories", "selection_criteria", "excluded_categories"];

    [JsonPropertyName("properties")]
    public SchemaProperties Properties { get; set; } = new() 
    { 
        SelectionCriteria = new TypeSchema { Type = "string" },
        ExcludedCategories = new ArraySchema
        {
            Type = "array",
            Items = new TypeSchema { Type = "string" }
        },
        TopStories = new ArraySchema
        {
            Type = "array",
            MinItems = 10,
            MaxItems = 10,
            Items = new ObjectSchema
            {
                Type = "object",
                Required = ["url", "headline", "category", "highlights", "rationale"],
                Properties = new Dictionary<string, TypeSchema>
                {
                    ["url"] = new TypeSchema { Type = "string" },
                    ["headline"] = new TypeSchema { Type = "string" },
                    ["category"] = new TypeSchema { Type = "string" },
                    ["highlights"] = new TypeSchema { Type = "string" },
                    ["rationale"] = new TypeSchema { Type = "string" }
                },
                AdditionalProperties = false
            }
        }
    };

    [JsonPropertyName("additionalProperties")]
    public bool AdditionalProperties { get; set; } = false;
}

public class SchemaProperties
{
    [JsonPropertyName("top_stories")]
    public ArraySchema TopStories { get; set; }

    [JsonPropertyName("selection_criteria")]
    public TypeSchema SelectionCriteria { get; set; }

    [JsonPropertyName("excluded_categories")]
    public ArraySchema ExcludedCategories { get; set; }
}

public class ArraySchema
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("minItems")]
    public int? MinItems { get; set; }

    [JsonPropertyName("maxItems")]
    public int? MaxItems { get; set; }

    [JsonPropertyName("items")]
    public object Items { get; set; } // Can be TypeSchema or ObjectSchema
}

public class ObjectSchema
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("required")]
    public string[] Required { get; set; }

    [JsonPropertyName("properties")]
    public Dictionary<string, TypeSchema> Properties { get; set; }

    [JsonPropertyName("additionalProperties")]
    public bool AdditionalProperties { get; set; }
}

public class TypeSchema
{
    [JsonPropertyName("type")]
    public string Type { get; set; }
}
