using System.Text.Json.Serialization;

namespace SnapshotJob.Perplexity.Models.TopStories.Request;

/// <summary>
/// Represents a schema that defines the type information for an object.
/// </summary>
public class TypeSchema
{
    /// <summary>
    /// Gets the type identifier associated with the object.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = default!;
}
