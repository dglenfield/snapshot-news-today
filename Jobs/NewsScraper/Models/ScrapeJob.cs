using NewsScraper.Models.AssociatedPress.MainPage;
using NewsScraper.Serialization;
using System.Text.Json;

namespace NewsScraper.Models;

/// <summary>
/// Represents the single execution of a scraping job.
/// </summary>
public class ScrapeJob
{
    public long Id { get; set; }
    public required string SourceName { get; set; }
    public required Uri SourceUri { get; set; }
    public DateTime JobStartedOn { get; } = DateTime.UtcNow;
    public DateTime? JobFinishedOn { get; set; }
    public bool? Success { get; set; }
    public ScrapeException? ScrapeException { get; set; }
    public PageScrapeResult? PageScrapeResult { get; set; }
    public bool UseTestFile { get; set; }
    public string? TestFile { get; set; }

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
