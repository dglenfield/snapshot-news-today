using HtmlAgilityPack;

namespace SnapshotJob.Scrapers.MainPage.Sections;

public class BeWellScraper(HtmlNode documentNode) : PageSectionScraperBase(documentNode)
{
    public override string SectionName => "Be Well";
    public override string SectionXPath => "//div[@data-gtm-region='be well headline queue']";
    public override string HeadlinesXPath => ".//div[normalize-space(@class) = 'PagePromo']";

    protected override string? FindUnixTimestamp(HtmlNode articleNode)
        => articleNode.GetAttributeValue("data-updated-date-timestamp", "");
}
