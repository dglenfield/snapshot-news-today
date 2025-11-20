using System.Text.Json.Serialization;

namespace SnapshotJob.Perplexity.Models.ApiResponse;

/// <summary>
/// Represents a message containing a role and associated textual content for the response.
/// </summary>
public class Message
{
    /// <summary>
    /// Gets or sets the role associated with the message.
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; init; } = default!;

    /// <summary>
    /// Gets the main textual content associated with this object.
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; init; } = default!;
}
