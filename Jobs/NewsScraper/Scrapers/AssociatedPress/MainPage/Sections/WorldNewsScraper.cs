using HtmlAgilityPack;

namespace NewsScraper.Scrapers.AssociatedPress.MainPage.Sections;

public class WorldNewsScraper(HtmlNode documentNode) : PageSectionScraperBase(documentNode)
{
    public override string SectionName => "World News";
    public override string SectionXPath
        => "//div[normalize-space(@class) = 'PageListRightRailA' and .//h2/a[contains(normalize-space(text()), 'WORLD NEWS')]]";
    public override string ArticlesXPath => ".//div[normalize-space(@class) = 'PagePromo']";

    protected override string? GetContentUnixTimestamp(HtmlNode articleNode)
        => articleNode.GetAttributeValue("data-updated-date-timestamp", "");
}
