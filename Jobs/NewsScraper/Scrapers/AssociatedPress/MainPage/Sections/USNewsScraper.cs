using HtmlAgilityPack;

namespace NewsScraper.Scrapers.AssociatedPress.MainPage.Sections;

public class USNewsScraper(HtmlNode documentNode) : PageSectionScraperBase(documentNode)
{
    public override string SectionName => "US News";
    public override string SectionXPath => "//div[@data-gtm-topic='Topics - US News']";
    public override string ArticlesXPath => ".//div[normalize-space(@class) = 'PagePromo']";

    protected override string? GetContentUnixTimestamp(HtmlNode articleNode)
        => articleNode.GetAttributeValue("data-updated-date-timestamp", "");
}
