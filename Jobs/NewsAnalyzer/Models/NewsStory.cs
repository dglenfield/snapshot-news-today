namespace NewsAnalyzer.Models;

//  https://snapshots.azurewebsites.net/1759860432 - Unix timestamp used for the page snapshot
// {
//   "CuratedHeadline": "Government Shutdown Continues Amid Budget Dispute, Affecting Millions of Americans",
//   "Highlights": "Federal government shutdown due to failed funding agreement between Republicans and Democrats; impacts federal workers, public services, and agencies; health care provisions at core of the dispute.",
//   "Rationale": "Government shutdowns have widespread immediate national impact, affecting federal employees, public services, and economic stability, making it a critical ongoing political and societal event.",
//   "CuratedCategory": "Politics",
//   "SourceUri": "https://www.cnn.com/2025/10/06/politics/government-shutdown-ending-scenarios",
//   "SourceHeadline": "How could the government shutdown end? Here are 4 scenarios",
//   "SourcePublishDate": "2025-10-06T00:00:00",
//   "SourceName": "CNN",
//   "SourceCategory": "politics"
// }

internal class NewsStory
{
    // SourceArticle
    // CuratedArticle
}

internal class NewsStoryCollection
{
    public List<NewsStory> Stories { get; set; } = new();
}

internal class Source
{
    public Uri Uri { get; set; } = default!;
    public string? Headline { get; set; }
    public DateTime? PublishDate { get; set; }
    public string? SourceName { get; set; }
    public string? Category { get; set; }
}

internal class CuratedArticle
{
    public string CuratedHeadline { get; set; } = default!;
    public string Highlights { get; set; } = default!;
    public string Rationale { get; set; } = default!;
    public string CuratedCategory { get; set; } = default!;
}
