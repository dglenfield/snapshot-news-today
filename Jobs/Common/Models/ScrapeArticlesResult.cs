using Common.Serialization;
using System.Text.Json;

namespace Common.Models;

public class ScrapeArticlesResult
{
    public DateTime? StartedOn { get; set; }
    public DateTime? FinishedOn { get; set; }
    public bool? IsSuccess { get; set; }
    public List<JobException>? ScrapeExceptions { get; set; }
    public List<ScrapedArticle>? ScrapedArticles { get; set; }

    public int ArticlesScraped => 
        ScrapedArticles is null ? 0 : ScrapedArticles.Count(a => a.Id > 0 && a.IsSuccess);

    public decimal? RunTimeInSeconds => StartedOn.HasValue && FinishedOn.HasValue ?
        (decimal)((long)(FinishedOn.Value - StartedOn.Value).TotalMilliseconds) / 1000 : null;

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
