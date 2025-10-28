using Common.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewsScraper.Models.CNN;

/// <summary>
/// Represents a news article from CNN and its content.
/// </summary>
/// <remarks>
/// This class is for backwards compatibilty with the existing CNN scraping process.
/// </remarks>
public class Article
{
    public Uri ArticleUri { get; set; } = default!;
    public string? Author { get; set; }
    public string? Category
    {
        get;
        // If the value is exactly two characters long, it is converted to uppercase (e.g., "us" becomes "US").
        // Otherwise, only the first character is capitalized (e.g., "business" becomes "Business").
        set => field = string.IsNullOrEmpty(value) ? value
            : (value.Length == 2 ? value.ToUpper() : char.ToUpper(value[0]) + value.Substring(1));
    }
    public List<string> ContentParagraphs { get; set; } = [];
    public string? ErrorMessage { get; set; }
    public string? Headline { get; set; }
    public long Id { get; set; }
    public long JobRunId { get; set; }
    public DateTime? LastUpdatedDate { get; set; }
    public DateTime? PublishDate { get; set; }
    public string? SourceName { get; set; }
    public bool? Success { get; set; }

    [JsonIgnore]
    public string Content => string.Join("\n\n", ContentParagraphs);

    public string GetContentParagraphs(int maxParagraphs)
    {
        if (maxParagraphs <= 0 || ContentParagraphs.Count == 0)
            return string.Empty;

        StringBuilder builder = new();
        for (int i = 0; i < maxParagraphs && i < ContentParagraphs.Count; i++)
            builder.AppendLine($"  [{i + 1}] {ContentParagraphs[i]}");

        return builder.ToString();
    }

    public override bool Equals(object? obj)
    {
        return obj is Article other && ArticleUri?.Equals(other.ArticleUri) == true;
    }

    public override int GetHashCode()
    {
        return ArticleUri?.GetHashCode() ?? 0;
    }

    /// <summary>
    /// Returns a JSON-formatted string that represents the current object.
    /// </summary>
    /// <remarks>
    /// The returned JSON string uses default serialization options and omits properties with null
    /// values for readability. This method is useful for logging, debugging, or exporting the object's state.
    /// </remarks>
    /// <returns>
    /// A string containing the JSON representation of the object, formatted with indentation and excluding properties
    /// with null values.
    /// </returns>
    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
