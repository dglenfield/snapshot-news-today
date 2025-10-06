using System.Text.Json.Serialization;

namespace NewsScraper.Models.PerplexityApi.Common.Request;

/// <summary>
/// Represents a JSON schema definition for an array type, including constraints on the number and structure of items.
/// </summary>
public class ArraySchema
{
    /// <summary>
    /// Gets the type of the JSON schema element which is always "array".
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get => "array"; }

    /// <summary>
    /// Gets the minimum number of items that are allowed in the collection.
    /// </summary>
    [JsonPropertyName("minItems")]
    public int? MinItems { get; init; }

    /// <summary>
    /// Gets the maximum number of items to include in the result.
    /// </summary>
    [JsonPropertyName("maxItems")]
    public int? MaxItems { get; init; }

    /// <summary>
    /// Gets the schema definition for items within a collection property.
    /// </summary>
    /// <remarks>The value can be either a <see cref="TypeSchema"/> or an <see cref="ObjectSchema"/>,
    /// depending on whether the items are of a simple type or a complex object. Use this property to determine the
    /// expected structure of elements in the collection.</remarks>
    [JsonPropertyName("items")]
    public object Items { get; init; } // Can be TypeSchema or ObjectSchema
}
