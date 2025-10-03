using NewsScraper.Serialization;
using System.Text.Json.Serialization;

namespace NewsScraper.Models.PerplexityApi.Requests;

/// <summary>
/// Represents a chat message.
/// </summary>
internal class Message
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

    internal Message(Role role, string content)
    {
        Content = content.ReplaceLineEndings(" ").Trim();
        Role = role;
    }
}

/// <summary>
/// Participant types in a chat conversation.
/// </summary>
internal enum Role
{
    Assistant,
    System,
    User
}
