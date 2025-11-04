using Common.Models;
using Common.Models.AssociatedPress.MainPage;
using HtmlAgilityPack;

namespace NewsSnapshot.Scrapers.AssociatedPress.MainPage.Sections;

public abstract class PageSectionScraperBase(HtmlNode documentNode) : IPageSectionScraper
{
    public abstract string SectionName { get; }
    public abstract string SectionXPath { get; }
    public abstract string HeadlinesXPath { get; }

    protected virtual Uri FindTargetUri(HtmlNode headlineNode)
        => new(headlineNode.SelectSingleNode(".//a[@href]").GetAttributeValue("href", ""));
    protected virtual string? FindTitle(HtmlNode headlineNode)
        => headlineNode.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText.Trim();
    protected virtual string? FindUnixTimestamp(HtmlNode headlineNode)
        => headlineNode.SelectSingleNode(".//div[normalize-space(@class) = 'PagePromo']")?.GetAttributeValue("data-updated-date-timestamp", "");

    protected virtual HtmlNode SectionNode => documentNode.SelectSingleNode(SectionXPath) 
        ?? throw new NodeNotFoundException($"{SectionName} section node not found. XPath failed for {SectionXPath}");
    protected virtual HtmlNodeCollection HeadlineNodes => SectionNode.SelectNodes(HeadlinesXPath) 
        ?? throw new NodeNotFoundException($"{SectionName} section node not found. XPath failed for {HeadlinesXPath}");

    public ScrapeSectionResult Scrape()
    {
        ScrapeSectionResult result = new() { SectionName = SectionName };
        HashSet<APNewsHeadline> headlines = [];

        try
        {
            // Hook for pre-processing - contains no default processing if not overriden
            PreProcessSection(headlines);
            // Hook for primary processing - contains the default processing if not overridden
            ProcessSection(headlines);
            // Hook for additional processing - contains no default processing if not overriden
            PostProcessSection(headlines);
        }
        catch (NodeNotFoundException ex)
        {
            result.ScrapeException = new JobException() { Source = $"Scraping section {SectionName} XPath error", Exception = ex };
        }
        catch (Exception ex) 
        {
            result.ScrapeException = new JobException() { Source = $"Scraping section {SectionName}", Exception = ex };
        }

        result.Headlines = headlines;
        return result;
    }

    // Pre-processing Hook - no default processing
    protected virtual void PreProcessSection(HashSet<APNewsHeadline> headlines) { }

    // Primary Processing Hook - contains default processing
    protected virtual void ProcessSection(HashSet<APNewsHeadline> headlines) 
    {
        foreach (var headlineNode in HeadlineNodes)
            headlines.Add(GetHeadline(headlineNode));
    }

    // Additional Processing Hook - no default processing
    protected virtual void PostProcessSection(HashSet<APNewsHeadline> headlines) { }

    protected virtual APNewsHeadline GetHeadline(HtmlNode headlineNode)
    {
        string? unixTimestamp = FindUnixTimestamp(headlineNode);
        return new()
        {
            SectionName = SectionName,
            TargetUri = FindTargetUri(headlineNode),
            Title = FindTitle(headlineNode),
            LastUpdatedOn = string.IsNullOrWhiteSpace(unixTimestamp) ? null : ConvertUnixTimestamp(unixTimestamp)
        };
    }

    protected DateTime? ConvertUnixTimestamp(string unixTimestamp)
    {
        if (long.TryParse(unixTimestamp, out long timestamp))
        {
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
            return dateTimeOffset.UtcDateTime;
        }
        return null;
    }
}
