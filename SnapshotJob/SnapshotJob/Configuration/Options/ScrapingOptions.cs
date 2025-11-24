using SnapshotNewsToday.Common.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace SnapshotJob.Configuration.Options;

public class ScrapingOptions
{
    public const string SectionName = "Scraping";

    [Required]
    public Uri BaseUri { get; set; } = default!;

    [Required]
    public string ArticleTestFile { get; set; } = default!;

    [Required]
    public string MainPageTestFile { get; set; } = default!;

    [Required]
    public bool UseArticleTestFile { get; set; }

    [Required]
    public bool UseMainPageTestFile { get; set; }

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
