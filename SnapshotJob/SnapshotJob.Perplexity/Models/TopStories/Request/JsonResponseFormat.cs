using System.Text.Json.Serialization;

namespace SnapshotJob.Perplexity.Models.TopStories.Request;

/// <summary>
/// The formatting to use for the response.
/// </summary>
public class JsonResponseFormat
{
    /// <summary>
    /// Gets the response format type identifier required for JSON schema responses.
    /// </summary>
    /// <remarks>This property always returns the string "json_schema". Use this value to indicate that the
    /// response format follows the JSON schema specification, as required by the API.</remarks>
    public string Type => "json_schema";

    /// <summary>
    /// The schema defining the expected structure of the JSON response.  
    /// </summary>
    [JsonPropertyName("json_schema")]
    public JsonSchema Schema { get; init; } = new();
}
