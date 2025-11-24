using SnapshotNewsToday.Common.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SnapshotJob.Perplexity.Models.AnalyzeArticle;

public class AnalyzeArticleContent
{
    [JsonPropertyName("custom_headline")]
    public string CustomHeadline { get; init; } = default!;

    [JsonPropertyName("summary")]
    public string Summary { get; init; } = default!;

    [JsonPropertyName("key_points")]
    public List<string> KeyPoints { get; init; } = default!;

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
