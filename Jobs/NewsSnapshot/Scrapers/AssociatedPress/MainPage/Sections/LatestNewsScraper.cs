using HtmlAgilityPack;

namespace NewsSnapshot.Scrapers.AssociatedPress.MainPage.Sections;

public class LatestNewsScraper(HtmlNode documentNode) : PageSectionScraperBase(documentNode)
{
    public override string SectionName => "Latest News";
    public override string SectionXPath => "//bsp-list-loadmore[@data-gtm-region='Most Recent']";
    public override string HeadlinesXPath => ".//div[normalize-space(@class) = 'PagePromo']";

    protected override string? FindUnixTimestamp(HtmlNode headlineNode)
        => headlineNode.GetAttributeValue("data-updated-date-timestamp", "");
}
