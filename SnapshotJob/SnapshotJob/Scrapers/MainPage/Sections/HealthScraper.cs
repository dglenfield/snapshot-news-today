using HtmlAgilityPack;

namespace SnapshotJob.Scrapers.MainPage.Sections;

public class HealthScraper(HtmlNode documentNode) : PageSectionScraperBase(documentNode)
{
    public override string SectionName => "Health";
    public override string SectionXPath => "//div[@data-gtm-topic='Topics - Be Well']";
    public override string HeadlinesXPath => ".//div[normalize-space(@class) = 'PagePromo']";

    protected override string? FindUnixTimestamp(HtmlNode headlineNode)
        => headlineNode.GetAttributeValue("data-updated-date-timestamp", "");
}
