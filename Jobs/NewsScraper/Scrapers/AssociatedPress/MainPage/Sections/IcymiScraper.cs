using HtmlAgilityPack;

namespace NewsScraper.Scrapers.AssociatedPress.MainPage.Sections;

public class IcymiScraper(HtmlNode documentNode) : PageSectionScraperBase(documentNode)
{
    public override string SectionName => "ICYMI";
    public override string SectionXPath => "//div[@data-tb-region='ICYMI']";
    public override string ArticlesXPath => ".//div[normalize-space(@class) = 'PagePromo']";

    protected override string? GetContentUnixTimestamp(HtmlNode articleNode)
        => articleNode.GetAttributeValue("data-updated-date-timestamp", "");
}
