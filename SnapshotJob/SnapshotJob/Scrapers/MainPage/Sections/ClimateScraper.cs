using HtmlAgilityPack;

namespace SnapshotJob.Scrapers.MainPage.Sections;

public class ClimateScraper(HtmlNode documentNode) : PageSectionScraperBase(documentNode)
{
    public override string SectionName => "Climate";
    public override string SectionXPath => "//div[@data-gtm-topic='Topics - Climate']";
    public override string HeadlinesXPath => ".//div[normalize-space(@class) = 'PagePromo']";

    protected override string? FindUnixTimestamp(HtmlNode headlineNode)
        => headlineNode.GetAttributeValue("data-updated-date-timestamp", "");
}
