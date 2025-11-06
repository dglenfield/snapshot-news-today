using Common.Serialization;
using System.Text.Json;

namespace Common.Models.Scraping.Results;

public class ScrapeHeadlinesResult
{
    public string Source { get; set; } = default!;
    public DateTime? StartedOn { get; set; }
    public DateTime? FinishedOn { get; set; }
    public bool? IsSuccess { get; set; }
    public List<JobException>? ScrapeExceptions { get; set; }
    public HashSet<ScrapedHeadline>? ScrapedHeadlines { get; set; }

    public int HeadlinesScraped => 
        ScrapedHeadlines is null ? 0 : ScrapedHeadlines.Count(h => h.Id > 0);
    public int SectionsScraped => 
        ScrapedHeadlines is null ? 0 : ScrapedHeadlines.DistinctBy(a => a.SectionName).Count();

    public decimal? RunTimeInSeconds => StartedOn.HasValue && FinishedOn.HasValue ?
        (decimal)((long)(FinishedOn.Value - StartedOn.Value).TotalMilliseconds) / 1000 : null;

    public void AddScrapeSectionResult(ScrapeSectionResult result)
    {
        foreach (var headline in result.Headlines)
        {
            ScrapedHeadlines ??= [];
            ScrapedHeadlines.Add(headline);
        }

        if (result.ScrapeException is not null)
        {
            ScrapeExceptions ??= [];
            ScrapeExceptions.Add(result.ScrapeException);
        }
            
    }

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
