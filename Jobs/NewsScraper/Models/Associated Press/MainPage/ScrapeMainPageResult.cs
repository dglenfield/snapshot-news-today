using NewsScraper.Serialization;
using System.Text.Json;

namespace NewsScraper.Models.AssociatedPress.MainPage;

public class ScrapeMainPageResult
{
    public DateTime? ScrapedOn { get; set; }
    public int SectionsScraped => Headlines.DistinctBy(a => a.SectionName).Count();
    public int HeadlinesScraped => Headlines.Count;
    public HashSet<Headline> Headlines { get; set; } = [];
    public List<ScrapeException> ScrapeExceptions { get; set; } = [];

    public void AddScrapeSectionResult(ScrapeSectionResult result)
    {
        foreach (var headline in result.Headlines)
            Headlines.Add(headline);

        if (result.ScrapeException is not null)
            ScrapeExceptions.Add(result.ScrapeException);
    }

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
