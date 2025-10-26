using NewsScraper.Models.AssociatedPress.MainPage;
using NewsScraper.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewsScraper.Models.AssociatedPress.ArticlePage;

public class Article
{
    public long Id { get; set; }

    public required long HeadlineId { get; set; }

    public DateTime? ScrapedOn { get; set; }

    public string? Headline { get; set; }
    
    public Uri SourceUri { get; set; } = default!;

    public string? TestFile { get; set; }

    public DateTime? LastUpdatedOn { get; set; } // UTC time

    public string? Author { get; set; }

    public List<string>? ContentParagraphs { get; set; }

    public bool IsSuccess { get; set; } = false;

    [JsonIgnore]
    public ScrapeException? ScrapeException { get; set; }

    [JsonIgnore]
    public string Content => string.Join("\n\n", ContentParagraphs!);

    public string GetContentParagraphs(int maxParagraphs)
    {
        if (maxParagraphs <= 0 || ContentParagraphs?.Count == 0)
            return string.Empty;

        StringBuilder builder = new();
        for (int i = 0; i < maxParagraphs && i < ContentParagraphs?.Count; i++)
            builder.AppendLine($"  [{i + 1}] {ContentParagraphs[i]}");

        return builder.ToString();
    }

    public override bool Equals(object? obj)
    {
        return obj is Article other && SourceUri?.Equals(other.SourceUri) == true;
    }

    /// <summary>
    /// Returns the hash code for this <see cref="Article"/> instance.
    /// </summary>
    /// <remarks>
    /// The hash code is based solely on the <see cref="SourceUri"/> property to ensure that articles
    /// with the same URI are considered equal when used in hash-based collections such as dictionaries
    /// or hash sets. Returns 0 if <see cref="SourceUri"/> is <see langword="null"/>.
    /// </remarks>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override int GetHashCode()
    {
        return SourceUri?.GetHashCode() ?? 0;
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
