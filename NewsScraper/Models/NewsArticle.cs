using NewsScraper.Serialization;
using System.Text.Json;

namespace NewsScraper.Models;

internal class NewsArticle
{
    public Uri SourceUri { get; set; } = default!;
    public string? SourceHeadline { get; set; }
    public DateTime? SourcePublishDate { get; set; }
    public string? SourceName { get; set; }
    public string? SourceCategory { get; set; }

    public string ToJson() => JsonSerializer.Serialize(this);
    public string ToJson(JsonSerializerOptions options) => JsonSerializer.Serialize(this, options);
    public string ToJson(JsonSerializerOptions options, CustomJsonSerializerOptions customOptions) =>
        JsonSerializer.Serialize(this, JsonConfig.Customize(options, customOptions));

    public override string ToString() => ToJson(JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
