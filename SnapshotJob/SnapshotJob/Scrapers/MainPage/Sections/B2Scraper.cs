using HtmlAgilityPack;

namespace SnapshotJob.Scrapers.MainPage.Sections;

public class B2Scraper(HtmlNode documentNode) : PageSectionScraperBase(documentNode)
{
    public override string SectionName => "B2";
    public override string SectionXPath => "//div[normalize-space(@class) = 'PageListRightRailA' and @data-tb-region='B2']";
    public override string HeadlinesXPath => ".//div[normalize-space(@class) = 'PagePromo']";

    protected override string? FindUnixTimestamp(HtmlNode articleNode)
        => articleNode.GetAttributeValue("data-updated-date-timestamp", "");
}
