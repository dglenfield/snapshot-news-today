using System.Text.Json.Serialization;

namespace NewsAnalyzer.Models.PerplexityApi.Common.Response;

/// <summary>
/// Represents a partial message update containing the role of the sender and the associated content.
/// </summary>
internal class Delta
{
    /// <summary>
    /// Gets the role associated with the message sender.
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; init; } = default!;

    /// <summary>
    /// Gets the content associated with this instance.
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; init; } = default!;
}
