using HtmlAgilityPack;
using NewsScraper.Models.AssociatedPress.MainPage;

namespace NewsScraper.Scrapers.AssociatedPress.MainPage.Sections;

public class MainStoryScraper(HtmlNode documentNode, string region) : PageSectionScraperBase(documentNode)
{
    public override string SectionName => $"{region} Main Story";
    public override string SectionXPath => $"//div[normalize-space(@class) = 'PageListStandardE' and @data-tb-region='{region}']";
    public override string ArticlesXPath => ".//div[normalize-space(@class) = 'PagePromo']";
    protected override string? GetContentUnixTimestamp(HtmlNode articleNode)
        => articleNode.SelectSingleNode(".//bsp-timestamp[@data-timestamp]")?.GetAttributeValue("data-timestamp", "");

    protected override void PreProcessSection(PageSection section)
    {
        // Get the main article
        var mainArticleNode = SectionNode.SelectSingleNode(".//div[normalize-space(@class) = 'PageListStandardE-leadPromo-info']");
        section.Content.Add(GetContent(mainArticleNode));
    }
}
