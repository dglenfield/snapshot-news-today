using SnapshotNewsToday.Common.Serialization;
using System.Text.Json.Serialization;

namespace SnapshotJob.Perplexity.Models.TopStories.Request;

/// <summary>
/// Represents a chat message.
/// </summary>
public class Message
{
    /// <summary>
    /// The contents of the message.
    /// </summary>
    [JsonPropertyOrderAttribute(1)]
    public string Content { get; private init; }

    /// <summary>
    /// The role of the speaker in this message.
    /// </summary>
    [JsonPropertyOrderAttribute(0)]
    [JsonConverter(typeof(LowercaseJsonStringEnumConverter))]
    public Role Role { get; private init; }

    /// <summary>
    /// Initializes a new instance of the Message class with the specified role and content.
    /// </summary>
    /// <param name="role">The role associated with the message. Determines the sender or context of the message.</param>
    /// <param name="content">The content of the message. Leading and trailing whitespace is trimmed, and line endings are replaced with
    /// spaces.</param>
    public Message(Role role, string content)
    {
        Content = content.ReplaceLineEndings(" ").Trim();
        Role = role;
    }
}

/// <summary>
/// Participant types in a chat conversation.
/// </summary>
public enum Role
{
    Assistant,
    System,
    User
}
