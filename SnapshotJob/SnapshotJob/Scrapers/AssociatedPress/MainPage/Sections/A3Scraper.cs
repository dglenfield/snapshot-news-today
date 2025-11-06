using HtmlAgilityPack;

namespace SnapshotJob.Scrapers.AssociatedPress.MainPage.Sections;

public class A3Scraper(HtmlNode documentNode) : PageSectionScraperBase(documentNode)
{
    public override string SectionName => "A3";
    public override string SectionXPath => "//bsp-list-loadmore[normalize-space(@class) = 'PageListStandardB' and @data-tb-region = 'A3']";
    public override string HeadlinesXPath => ".//div[normalize-space(@class) = 'PageList-items-item']";
}
