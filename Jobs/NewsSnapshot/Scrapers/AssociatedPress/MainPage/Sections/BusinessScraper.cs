using HtmlAgilityPack;

namespace NewsSnapshot.Scrapers.AssociatedPress.MainPage.Sections;

public class BusinessScraper(HtmlNode documentNode) : PageSectionScraperBase(documentNode)
{
    public override string SectionName => "Business";
    public override string SectionXPath => "//div[@data-gtm-topic='Topics - Business']";
    public override string HeadlinesXPath => ".//div[normalize-space(@class) = 'PagePromo']";

    protected override string? FindUnixTimestamp(HtmlNode articleNode)
        => articleNode.GetAttributeValue("data-updated-date-timestamp", "");
}
