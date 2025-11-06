using HtmlAgilityPack;

namespace SnapshotJob.Scrapers.AssociatedPress.MainPage.Sections;

public class SportsScraper(HtmlNode documentNode) : PageSectionScraperBase(documentNode)
{
    public override string SectionName => "Sports";
    public override string SectionXPath
        => "//div[normalize-space(@class) = 'PageListRightRailA' and .//h2/a[contains(normalize-space(text()), 'SPORTS')]]";
    public override string HeadlinesXPath => ".//div[normalize-space(@class) = 'PagePromo']";

    protected override string? FindUnixTimestamp(HtmlNode headlineNode)
        => headlineNode.GetAttributeValue("data-updated-date-timestamp", "");
}
