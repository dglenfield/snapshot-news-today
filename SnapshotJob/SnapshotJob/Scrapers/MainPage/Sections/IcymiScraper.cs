using HtmlAgilityPack;

namespace SnapshotJob.Scrapers.MainPage.Sections;

public class IcymiScraper(HtmlNode documentNode) : PageSectionScraperBase(documentNode)
{
    public override string SectionName => "ICYMI";
    public override string SectionXPath => "//div[@data-tb-region='ICYMI']";
    public override string HeadlinesXPath => ".//div[normalize-space(@class) = 'PagePromo']";

    protected override string? FindUnixTimestamp(HtmlNode headlineNode)
        => headlineNode.GetAttributeValue("data-updated-date-timestamp", "");
}
