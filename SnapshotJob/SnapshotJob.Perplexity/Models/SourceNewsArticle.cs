using SnapshotJob.Common.Serialization;
using System.Text.Json;

namespace SnapshotJob.Perplexity.Models;

public class SourceNewsArticle
{
    public string Id { get; set; } = default!;
    public Uri SourceUri { get; set; } = default!;
    public string? Headline { get; set; }
    public DateTime? LastUpdatedOn { get; set; }
    public string? SourceName { get; set; }
    public string? Category { get; set; }

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
