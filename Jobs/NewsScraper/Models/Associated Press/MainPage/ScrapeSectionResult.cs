using Common.Models;
using Common.Serialization;
using System.Text.Json;

namespace NewsScraper.Models.AssociatedPress.MainPage;

public class ScrapeSectionResult
{
    public required string SectionName { get; set; }
    public int HeadlinesScraped => Headlines.Count;
    public HashSet<Headline> Headlines { get; set; } = [];
    public JobException? ScrapeException { get; set; }

    public bool IsSuccess => ScrapeException is null;

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
