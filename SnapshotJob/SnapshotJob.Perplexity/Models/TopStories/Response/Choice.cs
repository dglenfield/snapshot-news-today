using System.Text.Json.Serialization;

namespace SnapshotJob.Perplexity.Models.TopStories.Response;

/// <summary>
/// Represents a single response option, including its index, completion reason, and associated message or delta content.
/// </summary>
/// <remarks>A Choice typically corresponds to one possible result or alternative returned by a
/// response-generating system. The properties provide information about the position of the choice, the reason the
/// response was completed, and the content of the message or delta associated with this choice.</remarks>
public class Choice
{
    /// <summary>
    /// Gets the zero-based index associated with this item.
    /// </summary>
    [JsonPropertyName("index")]
    public int Index { get; init; }

    /// <summary>
    /// Gets the reason the response was finished.
    /// </summary>
    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; init; } = default!;

    /// <summary>
    /// Gets the message associated with this choice.
    /// </summary>
    [JsonPropertyName("message")]
    public Message Message { get; init; } = default!;

    /// <summary>
    /// Gets the delta content associated with this object.
    /// </summary>
    [JsonPropertyName("delta")]
    public Delta Delta { get; init; } = default!;
}
