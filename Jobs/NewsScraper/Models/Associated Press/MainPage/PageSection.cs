using NewsScraper.Serialization;
using System.Text.Json;

namespace NewsScraper.Models.AssociatedPress.MainPage;

public class PageSection(string name)
{
    public string Name { get; init; } = name;
    public HashSet<PageSectionContent> Content { get; set; } = [];
    public Exception? ScrapeException { get; set; }
    public string? ScrapeMessage { get; set; } 
    public bool? ScrapeSuccess { get; set; }

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
