using Common.Models.AssociatedPress.MainPage;
using HtmlAgilityPack;

namespace NewsSnapshot.Scrapers.AssociatedPress.MainPage.Sections;

public class MainStoryScraper(HtmlNode documentNode, string region) : PageSectionScraperBase(documentNode)
{
    public override string SectionName => $"{region} Main Story";
    public override string SectionXPath => $"//div[normalize-space(@class) = 'PageListStandardE' and @data-tb-region='{region}']";
    public override string HeadlinesXPath => ".//div[normalize-space(@class) = 'PagePromo']";
    protected override string? FindUnixTimestamp(HtmlNode articleNode)
        => articleNode.SelectSingleNode(".//bsp-timestamp[@data-timestamp]")?.GetAttributeValue("data-timestamp", "");

    protected override void PreProcessSection(HashSet<APNewsHeadline> headlines)
    {
        // Get the main article
        var mainArticleNode = SectionNode.SelectSingleNode(".//div[normalize-space(@class) = 'PageListStandardE-leadPromo-info']");
        headlines.Add(GetHeadline(mainArticleNode));
    }
}
