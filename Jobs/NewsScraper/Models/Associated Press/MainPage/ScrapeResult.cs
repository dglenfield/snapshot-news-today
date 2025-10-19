using NewsScraper.Serialization;
using System.Text.Json;

namespace NewsScraper.Models.AssociatedPress.MainPage;

public class ScrapeResult
{
    public required Uri SourceUri { get; set; }
    public DateTime? ScrapedOn { get; set; }
    public List<string> ScrapeExceptions { get; set; } = [];
    public List<string> ScrapeMessages { get; set; } = [];
    public List<PageSection> Sections { get; set; } = [];

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
