using HtmlAgilityPack;
using NewsScraper.Models.AssociatedPress.MainPage;

namespace NewsScraper.Scrapers.AssociatedPress.MainPage;

public interface IPageSectionScraper
{
    string SectionName { get; }
    PageSection Scrape();
}

public abstract class PageSectionScraperBase(HtmlNode documentNode) : IPageSectionScraper
{
    public abstract string SectionName { get; }
    public abstract string SectionXPath { get; }
    public abstract string ArticlesXPath { get; }

    protected virtual HtmlNode SectionNode => documentNode.SelectSingleNode(SectionXPath) 
        ?? throw new Exception($"[{SectionName} section]: XPath failed for {SectionXPath}");
    protected virtual HtmlNodeCollection ArticleNodes => SectionNode.SelectNodes(ArticlesXPath) 
        ?? throw new Exception($"[{SectionName} section]: XPath failed for {ArticlesXPath}");

    public virtual PageSection Scrape()
    {
        PageSection section = new(SectionName) { ScrapeSuccess = true };
        
        try
        {
            foreach (var articleNode in ArticleNodes)
                section.Content.Add(GetContent(articleNode));

            PostProcessSection(section); // Hook for additional processing
        }
        catch (Exception ex)
        {
            section.ScrapeMessage = ex.Message;
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
