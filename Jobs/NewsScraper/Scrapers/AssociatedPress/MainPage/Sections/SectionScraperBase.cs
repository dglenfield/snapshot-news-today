using HtmlAgilityPack;
using NewsScraper.Models.AssociatedPress.MainPage;

namespace NewsScraper.Scrapers.AssociatedPress.MainPage.Sections;

public abstract class PageSectionScraperBase(HtmlNode documentNode) : IPageSectionScraper
{
    public abstract string SectionName { get; }
    public abstract string SectionXPath { get; }
    public abstract string ArticlesXPath { get; }

    protected virtual HtmlNode SectionNode => documentNode.SelectSingleNode(SectionXPath) 
        ?? throw new NodeNotFoundException($"{SectionName} section node not found. XPath failed for {SectionXPath}");
    protected virtual HtmlNodeCollection ArticleNodes => SectionNode.SelectNodes(ArticlesXPath) 
        ?? throw new NodeNotFoundException($"{SectionName} section node not found. XPath failed for {ArticlesXPath}");

    public PageSection Scrape()
    {
        PageSection section = new(SectionName) { ScrapeSuccess = true };

        try
        {
            // Hook for pre-processing - contains no default processing if not overriden
            PreProcessSection(section); 
            // Hook for primary processing - contains the default processing if not overridden
            ProcessSection(section);
            // Hook for additional processing - contains no default processing if not overriden
            PostProcessSection(section); 
        }
        catch (NodeNotFoundException ex)
        {
            section.ScrapeException = ex;
            section.ScrapeMessage = $"XPath error. {ex.Message}";
            section.ScrapeSuccess = false;
        }
        catch (Exception ex) 
        {
            section.ScrapeException = ex;
            section.ScrapeMessage = $"Scrape failed. {ex.Message}";
            section.ScrapeSuccess = false;
        }

        return section;
    }

    protected virtual void PreProcessSection(PageSection section) { }
    protected virtual void ProcessSection(PageSection section) 
    {
        foreach (var articleNode in ArticleNodes)
            section.Content.Add(GetContent(articleNode));
    }
    protected virtual void PostProcessSection(PageSection section) { }

    protected virtual Uri? GetContentTargetUri(HtmlNode articleNode) 
        => new(articleNode.SelectSingleNode(".//a[@href]").GetAttributeValue("href", ""));
    protected virtual string? GetContentTitle(HtmlNode articleNode) 
        => articleNode.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText.Trim();
    protected virtual string? GetContentUnixTimestamp(HtmlNode articleNode) 
        => articleNode.SelectSingleNode(".//div[normalize-space(@class) = 'PagePromo']")?.GetAttributeValue("data-updated-date-timestamp", "");

    protected virtual PageSectionContent GetContent(HtmlNode contentNode)
    {
        string? unixTimestamp = GetContentUnixTimestamp(contentNode);
        return new()
        {
            TargetUri = GetContentTargetUri(contentNode),
            Title = GetContentTitle(contentNode),
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
