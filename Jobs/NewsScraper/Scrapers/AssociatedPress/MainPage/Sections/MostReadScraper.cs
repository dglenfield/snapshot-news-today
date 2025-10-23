using HtmlAgilityPack;

namespace NewsScraper.Scrapers.AssociatedPress.MainPage.Sections;

public class MostReadScraper(HtmlNode documentNode) : PageSectionScraperBase(documentNode)
{
    public override string SectionName => "Most Read";
    public override string SectionXPath => "//div[normalize-space(@class) = 'PageListRightRailA' and @data-tb-region='Most read']";
    public override string HeadlinesXPath => ".//li[normalize-space(@class) = 'PageList-items-item']";
}
