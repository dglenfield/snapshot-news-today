using HtmlAgilityPack;

namespace NewsScraper.Scrapers.AssociatedPress.MainPage.Sections;

public class A3Scraper(HtmlNode documentNode) : PageSectionScraperBase(documentNode)
{
    public override string SectionName => "A3";
    public override string SectionXPath => "//bsp-list-loadmore[normalize-space(@class) = 'PageListStandardB' and @data-tb-region = 'A3']";
    public override string ArticlesXPath => ".//div[normalize-space(@class) = 'PageList-items-item']";
}
