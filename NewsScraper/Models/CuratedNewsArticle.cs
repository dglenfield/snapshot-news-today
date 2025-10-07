using NewsScraper.Serialization;
using System.Text.Json;

namespace NewsScraper.Models;

internal class CuratedNewsArticle : NewsArticle
{
    public string CuratedHeadline { get; set; } = default!;
    public string Highlights { get; set; } = default!;
    public string Rationale { get; set; } = default!;
    public string CuratedCategory { get; set; } = default!;

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
