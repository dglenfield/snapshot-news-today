using HtmlAgilityPack;

namespace NewsScraper.Scrapers.AssociatedPress.MainPage.Sections;

public class ScienceScraper(HtmlNode documentNode) : PageSectionScraperBase(documentNode)
{
    public override string SectionName => "Science";
    public override string SectionXPath => "//div[@data-gtm-region='Topics - Science']";
    public override string ArticlesXPath => ".//div[normalize-space(@class) = 'PagePromo']";

    protected override string? GetContentUnixTimestamp(HtmlNode articleNode)
        => articleNode.GetAttributeValue("data-updated-date-timestamp", "");
}
