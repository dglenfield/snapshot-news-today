using HtmlAgilityPack;

namespace SnapshotJob.Scrapers.MainPage.Sections;

public class USNewsScraper(HtmlNode documentNode) : PageSectionScraperBase(documentNode)
{
    public override string SectionName => "US News";
    public override string SectionXPath => "//div[@data-gtm-topic='Topics - US News']";
    public override string HeadlinesXPath => ".//div[normalize-space(@class) = 'PagePromo']";

    protected override string? FindUnixTimestamp(HtmlNode headlineNode)
        => headlineNode.GetAttributeValue("data-updated-date-timestamp", "");
}
