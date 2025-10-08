using System.Text.Json.Serialization;

namespace NewsAnalyzer.Models.PerplexityApi.Common.Request;

/// <summary>
/// Represents a schema that defines the type information for an object.
/// </summary>
internal class TypeSchema
{
    /// <summary>
    /// Gets the type identifier associated with the object.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = default!;
}
