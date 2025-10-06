using System.Text.Json.Serialization;

namespace NewsScraper.Models.PerplexityApi.CurateArticles.Request;

/// <summary>
/// Represents the root JSON schema definition for the response object, including required properties and schema
/// metadata.
/// </summary>
internal class JsonSchema
{
    /// <summary>
    /// Gets the name of the root schema.
    /// </summary>
    public string Name { get => "top_stories"; }

    /// <summary>
    /// Gets the type of the root schema.
    /// </summary>
    /// <remarks>The value is always "object" for the root schema.</remarks>
    public string Type { get => "object"; } 

    /// <summary>
    /// Gets the list of property names that are required for the response.
    /// </summary>
    [JsonPropertyName("required")]
    public string[] Required { get => ["top_stories", "selection_criteria", "excluded_categories"]; }

    /// <summary>
    /// Gets the schema properties for the response.
    /// </summary>
    public SchemaProperties? Properties { get; init; }
    
    /// <summary>
    /// Gets a value indicating whether strict mode is enabled.
    /// </summary>
    public bool Strict { get => true; }
    
    /// <summary>
    /// Gets a value indicating whether additional properties are supported.
    /// </summary>
    public bool AdditionalProperties { get => false; }

    /// <summary>
    /// Initializes a new instance of the JsonSchema class.
    /// </summary>
    internal JsonSchema()
    {
        Properties = new();
    }
}
