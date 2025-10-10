using NewsScraper.Serialization;
using System.Text.Json;

namespace NewsScraper.Models;

/// <summary>
/// Represents a news article obtained from an external source, including metadata such as headline, publication date,
/// source name, and category.
/// </summary>
/// <remarks>Use this type to encapsulate information about a news article as provided by its original source. All
/// properties reflect the source's data and may be null if not available.</remarks>
public class NewsArticle
{
    public long Id { get; set; }
    public long JobRunId { get; set; }
    /// <summary>
    /// Gets or sets the URI of the source from which the new article is obtained.
    /// </summary>
    public Uri SourceUri { get; set; } = default!;

    /// <summary>
    /// Gets or sets the source's headline for this news article.
    /// </summary>
    public string? SourceHeadline { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this news article was published by the source.
    /// </summary>
    public DateTime? SourcePublishDate { get; set; }

    /// <summary>
    /// Gets or sets the name of the source for this news article.
    /// </summary>
    public string? SourceName { get; set; }

    /// <summary>
    /// Gets or sets the category identifier for the source, used to group or classify sources by type.
    /// </summary>
    /// <remarks>If the assigned value is a two-character string, it is converted to uppercase. For longer
    /// strings, only the first character is capitalized. If the value is null or empty, it is stored as-is.</remarks>
    public string? SourceCategory
    {
        get;
        set => field = string.IsNullOrEmpty(value) ? value : (value.Length == 2 ? value.ToUpper() : char.ToUpper(value[0]) + value.Substring(1));
    }

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
