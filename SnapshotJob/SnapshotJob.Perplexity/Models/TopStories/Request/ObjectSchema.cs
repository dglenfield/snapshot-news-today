using System.Text.Json.Serialization;

namespace SnapshotJob.Perplexity.Models.TopStories.Request;

/// <summary>
/// Represents the schema definition for an object type, including its required properties and property definitions.
/// </summary>
/// <remarks>The schema specifies the object's type identifier, required property names, property definitions, and
/// whether additional properties are permitted.</remarks>
public class ObjectSchema
{
    /// <summary>
    /// Gets the type identifier for the object represented by this instance.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type => "object";

    /// <summary>
    /// Gets the list of property names that are required for the associated object.
    /// </summary>
    /// <remarks>The list may be empty if no properties are required.</remarks>
    [JsonPropertyName("required")]
    public string[] Required { get; init; } = default!;

    /// <summary>
    /// Gets the collection of property definitions for the schema, keyed by property name.
    /// </summary>
    /// <remarks>Each entry in the dictionary represents a property defined in the schema, where the key is
    /// the property name and the value describes the property's type and constraints.</remarks>
    [JsonPropertyName("properties")]
    public Dictionary<string, TypeSchema> Properties { get; init; } = default!;

    /// <summary>
    /// Gets a value indicating whether additional properties are allowed beyond those explicitly defined.
    /// </summary>
    [JsonPropertyName("additionalProperties")]
    public bool AdditionalProperties { get; init; }
}
