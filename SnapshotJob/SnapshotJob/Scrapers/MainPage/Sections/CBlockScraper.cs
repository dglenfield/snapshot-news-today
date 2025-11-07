using HtmlAgilityPack;
using SnapshotJob.Data.Models;

namespace SnapshotJob.Scrapers.MainPage.Sections;

public class CBlockScraper(HtmlNode documentNode) : PageSectionScraperBase(documentNode)
{
    public override string SectionName => "C Block";
    public override string SectionXPath => "//div[normalize-space(@class) = 'PageListRightRailA' and @data-tb-region='C block']";
    public override string HeadlinesXPath => ".//li[normalize-space(@class) = 'PageList-items-item']";

    protected override void PreProcessSection(HashSet<ScrapedHeadline> headlines)
    {
        // Fetch the first headline which is the "lead" article for this group
        var leadHeadlineNode = SectionNode.SelectSingleNode("//div[normalize-space(@class) = 'PageList-items-first']");
        headlines.Add(GetHeadline(leadHeadlineNode));
    }
}
