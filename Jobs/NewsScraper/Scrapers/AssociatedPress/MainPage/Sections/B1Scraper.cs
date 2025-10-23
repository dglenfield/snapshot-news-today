using HtmlAgilityPack;

namespace NewsScraper.Scrapers.AssociatedPress.MainPage.Sections;

public class B1Scraper(HtmlNode documentNode) : PageSectionScraperBase(documentNode)
{
    public override string SectionName => "B1";
    public override string SectionXPath => "//div[normalize-space(@class) = 'PageListStandardE' and @data-tb-region='B1']";
    public override string HeadlinesXPath => ".//bsp-custom-headline";
}
