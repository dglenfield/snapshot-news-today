using SnapshotNewsToday.Common.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SnapshotJob.Perplexity.Configuration.Options;

public class PerplexityOptions
{
    public const string SectionName = "Perplexity";

    [Required]
    [JsonIgnore]
    public string ApiKey { get; set; } = default!;

    [Required]
    public Uri ApiUri { get; set; } = default!;

    [Required]
    public string ArticleTestFile { get; set; } = default!;

    [Required]
    public string TopStoriesTestFile { get; set; } = default!;

    [Required]
    public bool UseArticleTestFile { get; set; }

    [Required]
    public bool UseTopStoriesTestFile { get; set; }

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
