using HtmlAgilityPack;
using NewsScraper.Models.AssociatedPress.MainPage;

namespace NewsScraper.Scrapers.AssociatedPress.MainPage.Sections;

public class CBlockScraper(HtmlNode documentNode) : PageSectionScraperBase(documentNode)
{
    public override string SectionName => "C Block";
    public override string SectionXPath => "//div[normalize-space(@class) = 'PageListRightRailA' and @data-tb-region='C block']";
    public override string ArticlesXPath => ".//li[normalize-space(@class) = 'PageList-items-item']";

    protected override void PreProcessSection(PageSection section)
    {
        // Fetch the first article which is the "lead" article for this group
        var leadArticleNode = SectionNode.SelectSingleNode("//div[normalize-space(@class) = 'PageList-items-first']");
        section.Content.Add(GetContent(leadArticleNode));
    }
}
