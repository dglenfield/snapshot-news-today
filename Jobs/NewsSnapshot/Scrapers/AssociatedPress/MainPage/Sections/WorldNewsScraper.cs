using HtmlAgilityPack;

namespace NewsSnapshot.Scrapers.AssociatedPress.MainPage.Sections;

public class WorldNewsScraper(HtmlNode documentNode) : PageSectionScraperBase(documentNode)
{
    public override string SectionName => "World News";
    public override string SectionXPath
        => "//div[normalize-space(@class) = 'PageListRightRailA' and .//h2/a[contains(normalize-space(text()), 'WORLD NEWS')]]";
    public override string HeadlinesXPath => ".//div[normalize-space(@class) = 'PagePromo']";

    protected override string? FindUnixTimestamp(HtmlNode headlineNode)
        => headlineNode.GetAttributeValue("data-updated-date-timestamp", "");
}
