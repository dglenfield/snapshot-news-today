using NewsScraper.Serialization;
using System.Text.Json;

namespace NewsScraper.Models;

/// <summary>
/// Represents a news story obtained from a specific source, including its associated article details.
/// </summary>
public class SourceNewsStory
{
    public SourceArticle? Article { get; set; }
    public string? Category
    {
        get;
        // If the value is exactly two characters long, it is converted to uppercase (e.g., "us" becomes "US").
        // Otherwise, only the first character is capitalized (e.g., "business" becomes "Business").
        set => field = string.IsNullOrEmpty(value) ? value
            : (value.Length == 2 ? value.ToUpper() : char.ToUpper(value[0]) + value.Substring(1));
    }
    public long Id { get; set; }
    public long JobRunId { get; set; }
    public string? SourceName { get; set; }
    public string? StoryHeadline { get; set; }

    /// <summary>
    /// Returns a JSON-formatted string that represents the current object.
    /// </summary>
    /// <remarks>The returned JSON string uses default serialization options and omits properties with null
    /// values for readability. This method is useful for logging, debugging, or exporting the object's state.</remarks>
    /// <returns>A string containing the JSON representation of the object, formatted with indentation and excluding properties
    /// with null values.</returns>
    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default, 
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
