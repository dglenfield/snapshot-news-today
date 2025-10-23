using NewsScraper.Serialization;
using System.Text.Json;

namespace NewsScraper.Models.AssociatedPress.MainPage;

public class SectionScrapeResult
{
    public required string SectionName { get; set; }
    public int HeadlinesScraped => Headlines.Count;
    public HashSet<Headline> Headlines { get; set; } = [];
    public ScrapeException? ScrapeException { get; set; }
    public ScrapeMessage? Message { get; set; }

    public bool? ScrapeSuccess => ScrapeException is null;

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
