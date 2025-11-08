using System.Text.Json.Serialization;

namespace SnapshotJob.Perplexity.Models.TopStories.Request;

/// <summary>
/// Represents the schema definition for the top stories object, including its required properties and constraints.
/// </summary>
public class TopStoriesSchema
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
    public string[] Required => ["top_stories", "selection_criteria", "excluded_categories"];

    public TopStoriesPropertiesSchema Properties { get; init; } = new();

    /// <summary>
    /// Gets a value indicating whether additional properties are allowed beyond those explicitly defined.
    /// </summary>
    [JsonPropertyName("additionalProperties")]
    public bool AdditionalProperties => false;
}
