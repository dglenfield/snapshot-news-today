//using Common.Serialization;
//using System.Text.Json;

//namespace Common.Models;

//public class ScrapeMainPageResult
//{
//    public DateTime? ScrapedOn { get; set; }
//    public int SectionsScraped => ScrapedHeadlines.DistinctBy(a => a.SectionName).Count();
//    public int HeadlinesScraped => ScrapedHeadlines.Count(h => h.Id > 0);
//    public HashSet<ScrapedHeadline> ScrapedHeadlines { get; set; } = [];
//    public List<JobException> ScrapeExceptions { get; set; } = [];

//    public void AddScrapeSectionResult(ScrapeSectionResult result)
//    {
//        foreach (var headline in result.Headlines)
//            ScrapedHeadlines.Add(headline);

//        if (result.ScrapeException is not null)
//            ScrapeExceptions.Add(result.ScrapeException);
//    }

//    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
//        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
//}
