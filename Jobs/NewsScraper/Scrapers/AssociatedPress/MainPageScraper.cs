using Common.Logging;
using HtmlAgilityPack;
using NewsScraper.Models.AssociatedPress.MainPage;
using NewsScraper.Scrapers.AssociatedPress.MainPage;
using System.Text.RegularExpressions;
using static System.Collections.Specialized.BitVector32;

namespace NewsScraper.Scrapers.AssociatedPress;

internal class MainPageScraper(Logger logger)
{
    private readonly string _baseUrl = "https://apnews.com";
    private readonly string _testFile = @"C:/Users/danny/OneDrive/Projects/SnapshotNewsToday/TestData/AssociatedPressNews.html";
    private readonly bool _useTestFile = true;

    public async Task<ScrapeResult> Scrape()
    {
        ScrapeResult scrapeResult = new()
        {
            SourceUri = new Uri(_baseUrl),
            ScrapedOn = DateTime.UtcNow
        };

        HtmlDocument htmlDocument = new();
        if (_useTestFile)
            htmlDocument.Load(_testFile);
        else
            htmlDocument.LoadHtml(await new HttpClient().GetStringAsync(_baseUrl));

        var result = ProcessDocument(htmlDocument.DocumentNode);
        scrapeResult.Sections = result.Item1;
        foreach (var exception in result.Item2)
        {
            scrapeResult.ScrapeMessages.Add(exception.Message);
        }

        if (scrapeResult.Sections.Count == 0)
        {
            // Log the error if no sections returned
            scrapeResult.ScrapeMessages.Add("ERROR: No page sections found.");
            logger.LogException(new Exception("No page sections found."));
        }

        foreach (var section in scrapeResult.Sections) 
        {
            // Add any scrape messages to scrapeResult    
            if (section.ScrapeMessage is not null) 
            {
                logger.Log($"\n{section.Name}: {section.ScrapeMessage}");
            }
        }

        return scrapeResult;
    }

    public (List<PageSection>, List<Exception>) ProcessDocument(HtmlNode documentNode)
    {
        List<PageSection> pageSections = [];
        List<Exception> exceptions = [];

        //pageSections.Add(GetMainStory(documentNode, "A1"));
        //pageSections.Add(GetMainStory(documentNode, "A2"));
        
        pageSections.Add(new A3Scraper(documentNode).Scrape());
        pageSections.Add(new CBlockScraper(documentNode).Scrape());
        pageSections.Add(new MostReadScraper(documentNode).Scrape()); // TODO: Most Read can be duplicates of other articles so it's good to mark as "Most Read"
        pageSections.Add(new B1Scraper(documentNode).Scrape());




        //pageSections.Add(GetB2Section(documentNode));
        //pageSections.Add(GetIcymiSection(documentNode));
        //pageSections.Add(GetBeWellSection(documentNode));


        // US News Articles
        //articleCount += GetUSNewsArticles(htmlDoc);
        // World News Articles (AP News mislabels this section as "Topics - Sports")
        //articleCount += GetWorldNewsArticles(htmlDoc);
        // Politics Articles
        //articleCount += GetPoliticsArticles(htmlDoc);
        // Entertainment Articles
        //articleCount += GetEntertainmentArticles(htmlDoc);
        // Sports Articles (AP News has "Sports" as "Topics - World News")
        //articleCount += GetSportsArticles(htmlDoc);
        // Business Articles
        //articleCount += GetBusinessArticles(htmlDoc);
        // Science Articles
        //articleCount += GetScienceArticles(htmlDoc);
        // Lifestyle Articles
        //articleCount += GetLifestyleArticles(htmlDoc);
        // Technology Articles
        //articleCount += GetTechnologyArticles(htmlDoc);
        // Health Articles
        //articleCount += GetHealthArticles(htmlDoc);
        // Climate Articles
        //articleCount += GetClimateArticles(htmlDoc);
        // Fact Check Articles
        //articleCount += GetFactCheckArticles(htmlDoc);
        // Latest News Articles
        //articleCount += GetLatestNewsArticles(htmlDoc);

        // Get all "article" and "live" hyperlinks on the page
        var allLinks = GetAllHyperlinks(documentNode); // TODO: Compare articles found with all links

        int articleCount = 0;
        foreach (var section in pageSections)
        {
            logger.Log($"\n{section.Name}", logAsRawMessage: true);
            if (section.Content is null)
                continue;
            foreach (var article in section.Content)
            {
                articleCount++;
                if (article.LastUpdatedOn.HasValue)
                    logger.Log($"{article.LastUpdatedOn}", logAsRawMessage: true);
                logger.Log($"  {article.Title}", logAsRawMessage: true);
                logger.Log($"  {article.TargetUri}", logAsRawMessage: true);
            }
        }

        if (_useTestFile)
        {
            logger.Log($"\nUsing test file: {_testFile}", logAsRawMessage: true);
            logger.Log($"Total articles found: {articleCount}", logAsRawMessage: true);
            logger.Log($"Total hyperlinks found: {allLinks.Count}", logAsRawMessage: true);
        }
        else
        {
            logger.Log($"\nTotal articles found on {_baseUrl}: {articleCount}", logAsRawMessage: true);
            logger.Log($"Total hyperlinks found on {_baseUrl}: {allLinks.Count}", logAsRawMessage: true);
        }
        return (pageSections, exceptions);
    }

    private List<string> GetAllHyperlinks(HtmlNode documentNode)
    {
        return documentNode.SelectNodes("//a[@href]")?.Select(node => node.GetAttributeValue("href", ""))
            .Where(href =>
                href.StartsWith($"{_baseUrl}/article/", StringComparison.OrdinalIgnoreCase) ||
                href.StartsWith($"{_baseUrl}/live/", StringComparison.OrdinalIgnoreCase))
            .Distinct().ToList() ?? [];
    }

    private PageSection GetA1MainStory(HtmlNode documentNode) => GetMainStory(documentNode, "A1");
    private PageSection GetA2MainStory(HtmlNode documentNode) => GetMainStory(documentNode, "A2");
    private PageSection GetMainStory(HtmlNode documentNode, string region)
    {
        PageSection section = new($"{region} Main Story") { ScrapeSuccess = true };
        var sectionNode = documentNode.SelectSingleNode($"//div[normalize-space(@class) = 'PageListStandardE' and @data-tb-region = '{region}']");
        if (sectionNode is null)
        {
            section.ScrapeMessage= $"Unable to find a node with class='PageListStandardE' and @data-tb-region='{region}'";
            return section;
        }

        // Find the main article
        var mainArticleNode = sectionNode.SelectSingleNode(".//div[normalize-space(@class) = 'PageListStandardE-leadPromo-info']");
        PageSectionContent mainArticle = new()
        {
            TargetUri = new(mainArticleNode.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "")),
            Title = mainArticleNode.SelectSingleNode(".//span").InnerText.Trim()
        };
        section.Content.Add(mainArticle);

        // Find the secondary articles
        var articlesNode = sectionNode.SelectSingleNode(".//div[normalize-space(@class) = 'PageListStandardE-items-secondary']");
        if (articlesNode is null)
            return section;
            
        foreach (var articleNode in articlesNode.SelectNodes(".//div[normalize-space(@class) = 'PagePromo']"))
        {
            string? unixTimestamp = articleNode.SelectSingleNode(".//bsp-timestamp[@data-timestamp]")?.GetAttributeValue("data-timestamp", "");
            PageSectionContent article = new()
            {
                TargetUri = new(articleNode.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "")),
                Title = articleNode.SelectSingleNode(".//span").InnerText.Trim(),
                LastUpdatedOn = string.IsNullOrWhiteSpace(unixTimestamp) ? null : ConvertUnixTimestamp(unixTimestamp)
            };
            section.Content.Add(article);
        }
        return section;
    }

    //private PageSection GetA3Section(HtmlNode documentNode)
    //{
    //    PageSection section = new("A3") { ScrapeSuccess = true };
    //    var sectionNode = documentNode.SelectSingleNode("//bsp-list-loadmore[normalize-space(@class) = 'PageListStandardB' and @data-tb-region = 'A3']");
    //    if (sectionNode is null)
    //    {
    //        section.ScrapeMessage = $"Unable to find a node with class='PageListStandardB' and @data-tb-region='A3'";
    //        return section;
    //    }

    //    var articleNodes = sectionNode.SelectNodes(".//div[normalize-space(@class) = 'PageList-items-item']");
    //    foreach (var articleNode in articleNodes)
    //    {
    //        string? unixTimestamp = articleNode.SelectSingleNode(".//div[normalize-space(@class) = 'PagePromo']")?.GetAttributeValue("data-updated-date-timestamp", "");
    //        PageSectionContent article = new()
    //        {
    //            TargetUri = new(articleNode.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "")),
    //            Title = articleNode.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText.Trim(),
    //            LastUpdatedOn = string.IsNullOrWhiteSpace(unixTimestamp) ? null : ConvertUnixTimestamp(unixTimestamp)
    //        };
    //        section.Content.Add(article);
    //    }
    //    return section;
    //}

    public class A3Scraper(HtmlNode documentNode) : PageSectionScraperBase(documentNode) 
    {
        public override string SectionName => "A3";
        public override string SectionXPath => "//bsp-list-loadmore[normalize-space(@class) = 'PageListStandardB' and @data-tb-region = 'A3']";
        public override string ArticlesXPath => ".//div[normalize-space(@class) = 'PageList-items-item']";
    }

    public class CBlockScraper(HtmlNode documentNode) : PageSectionScraperBase(documentNode)
    {
        public override string SectionName => "C Block";
        public override string SectionXPath => "//div[normalize-space(@class) = 'PageListRightRailA' and @data-tb-region='C block']";
        public override string ArticlesXPath => ".//li[normalize-space(@class) = 'PageList-items-item']";

        public override PageSection Scrape()
        {
            PageSection section = new(SectionName) { ScrapeSuccess = true };

            // Fetch the first article which is the "lead" article for this group
            var leadArticleNode = SectionNode.SelectSingleNode("//div[normalize-space(@class) = 'PageList-items-first']");
            section.Content.Add(GetContent(leadArticleNode));

            // Find all the other articles for this group
            foreach (var articleNode in ArticleNodes)
                section.Content.Add(GetContent(articleNode));
            return section;
        }
    }

    public class MostReadScraper(HtmlNode documentNode) : PageSectionScraperBase(documentNode)
    {
        public override string SectionName => "Most Read";
        public override string SectionXPath => "//div[normalize-space(@class) = 'PageListRightRailA' and @data-tb-region='Most read']";
        public override string ArticlesXPath => ".//li[normalize-space(@class) = 'PageList-items-item']";
    }

    public class B1Scraper(HtmlNode documentNode) : PageSectionScraperBase(documentNode)
    {
        public override string SectionName => "B1";
        public override string SectionXPath => "//div[normalize-space(@class) = 'PageListStandardE' and @data-tb-region='B1']";
        public override string ArticlesXPath => ".//bsp-custom-headline";
    }

    //private PageSection ScrapePageSection(HtmlNode documentNode, IPageSectionScrapingStrategy strategy)
    //{
    //    PageSection section = new(strategy.SectionName) { ScrapeSuccess = true };
    //    foreach (var articleNode in strategy.GetArticleNodes(documentNode))
    //        section.Content.Add(GetContent(articleNode, strategy.GetContentTargetUri, strategy.GetContentTitle, strategy.GetContentUnixTimestamp));
    //    return section;
    //}
    //private PageSectionContent GetContent(HtmlNode contentNode, Func<HtmlNode, Uri?> GetTargetUri,
    //    Func<HtmlNode, string?> GetTitle, Func<HtmlNode, string?> GetUnixTimestamp)
    //{
    //    string? unixTimestamp = GetUnixTimestamp(contentNode);
    //    return new()
    //    {
    //        TargetUri = GetTargetUri(contentNode),
    //        Title = GetTitle(contentNode),
    //        LastUpdatedOn = string.IsNullOrWhiteSpace(unixTimestamp) ? null : ConvertUnixTimestamp(unixTimestamp)
    //    };
    //}

    // Method 2
    //public PageSection GetMostReadPageSection(HtmlNode documentNode) => GetPageSection(documentNode, new MostReadSectionConfig());
    //public interface IPageSectionScrapeConfig
    //{
    //    public string SectionName { get; }
    //    public string SectionXPath { get; }
    //    public string ArticlesXPath { get; }
    //    public abstract HtmlNode GetSectionNode(HtmlNode documentNode);
    //    public abstract HtmlNodeCollection GetArticleNodes(HtmlNode sectionNode);
    //    public abstract Uri? GetContentTargetUri(HtmlNode articleNode);
    //    public abstract string? GetContentTitle(HtmlNode articleNode);
    //    public abstract string? GetContentUnixTimestamp(HtmlNode articleNode);
    //}
    //public class MostReadSectionConfig : IPageSectionScrapeConfig
    //{
    //    public string SectionName => "Most Read";
    //    public string SectionXPath => "//div[normalize-space(@class) = 'PageListRightRailA' and @data-tb-region='Most read']";
    //    public string ArticlesXPath => ".//li[normalize-space(@class) = 'PageList-items-item']";
    //    public HtmlNode GetSectionNode(HtmlNode documentNode) => documentNode.SelectSingleNode(SectionXPath) ?? throw new Exception($"[{SectionName} section]: XPath failed for {SectionXPath}");
    //    public HtmlNodeCollection GetArticleNodes(HtmlNode sectionNode) => sectionNode.SelectNodes(ArticlesXPath) ?? throw new Exception($"[{SectionName} section]: XPath failed for {ArticlesXPath}");
    //    public Uri? GetContentTargetUri(HtmlNode articleNode) => new(articleNode.SelectSingleNode(".//a[@href]").GetAttributeValue("href", ""));
    //    public string? GetContentTitle(HtmlNode articleNode) => articleNode.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText.Trim();
    //    public string? GetContentUnixTimestamp(HtmlNode articleNode) => articleNode.SelectSingleNode(".//div[normalize-space(@class) = 'PagePromo']")?.GetAttributeValue("data-updated-date-timestamp", "");
    //}
    //private PageSection GetPageSection(HtmlNode documentNode, IPageSectionScrapeConfig config)
    //{
    //    PageSection section = new(config.SectionName) { ScrapeSuccess = true };
    //    //var sectionNode = documentNode.SelectSingleNode(config.SectionXPath) ?? throw new Exception($"[{section.Name} section]: XPath failed for {config.SectionXPath}");
    //    var sectionNode = config.GetSectionNode(documentNode);
    //    //var articleNodes = sectionNode.SelectNodes(config.ArticlesXPath) ?? throw new Exception($"[{section.Name} section]: XPath failed for {config.ArticlesXPath}");
    //    var articleNodes = config.GetArticleNodes(sectionNode);
    //    foreach (var articleNode in articleNodes)
    //        section.Content.Add(GetContent(articleNode, config.GetContentTargetUri, config.GetContentTitle, config.GetContentUnixTimestamp));
    //    return section;
    //}
    //private PageSectionContent GetContent(HtmlNode contentNode, Func<HtmlNode, Uri?> GetTargetUri, 
    //    Func<HtmlNode, string?> GetTitle, Func<HtmlNode, string?> GetUnixTimestamp)
    //{
    //    string? unixTimestamp = GetUnixTimestamp(contentNode);
    //    return new()
    //    {
    //        TargetUri = GetTargetUri(contentNode),
    //        Title = GetTitle(contentNode),
    //        LastUpdatedOn = string.IsNullOrWhiteSpace(unixTimestamp) ? null : ConvertUnixTimestamp(unixTimestamp)
    //    };
    //}

    private PageSection GetMostReadSection(HtmlNode documentNode)
    {
        PageSection section = new("Most Read") { ScrapeSuccess = true };
        var sectionNode = documentNode.SelectSingleNode("//div[normalize-space(@class) = 'PageListRightRailA' and @data-tb-region='Most read']");
        if (sectionNode is null)
        {
            section.ScrapeMessage = "Unable to find a node with class='PageListRightRailA' and @data-tb-region='Most read'";
            return section;
        }

        var articleNodes = sectionNode.SelectNodes(".//li[normalize-space(@class) = 'PageList-items-item']");
        foreach (var articleNode in articleNodes)
        {
            string? unixTimestamp = articleNode.SelectSingleNode(".//div[normalize-space(@class) = 'PagePromo']")?.GetAttributeValue("data-updated-date-timestamp", "");
            PageSectionContent article = new()
            {
                TargetUri = new(articleNode.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "")),
                Title = articleNode.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText.Trim(),
                LastUpdatedOn = string.IsNullOrWhiteSpace(unixTimestamp) ? null : ConvertUnixTimestamp(unixTimestamp)
            };
            section.Content.Add(article);
        }
        return section;
    }

    // Note: B1 Section content has no LastUpdatedOn
    //private PageSection GetB1Section(HtmlNode documentNode) =>
    //    GetSection(documentNode, sectionName: "B1",
    //        sectionXPath: "//div[normalize-space(@class) = 'PageListStandardE' and @data-tb-region='B1']",
    //        articlesXPath: ".//bsp-custom-headline");
    private PageSection GetB2Section(HtmlNode documentNode) =>
        GetSection(documentNode, sectionName: "B2",
            sectionXPath: "//div[normalize-space(@class) = 'PageListRightRailA' and @data-tb-region='B2']",
            articlesXPath: ".//div[normalize-space(@class) = 'PagePromo']");

    private PageSection GetIcymiSection(HtmlNode documentNode) =>
        GetSection(documentNode, sectionName: "ICYMI",
            sectionXPath: "//div[@data-tb-region='ICYMI']",
            articlesXPath: ".//div[normalize-space(@class) = 'PagePromo']");

    private PageSection GetBeWellSection(HtmlNode documentNode) => 
        GetSection(documentNode, sectionName: "Be Well",
            sectionXPath: "//div[@data-gtm-region='be well headline queue']", 
            articlesXPath: ".//div[normalize-space(@class) = 'PagePromo']");
    
    private PageSection GetSection(HtmlNode documentNode, string sectionName, string sectionXPath, string articlesXPath)
    {
        PageSection section = new(sectionName) { ScrapeSuccess = true };
        try
        {
            var sectionNode = documentNode.SelectSingleNode(sectionXPath) ?? throw new Exception($"[{section.Name} section]: XPath failed for {sectionXPath}");
            var articleNodes = sectionNode.SelectNodes(articlesXPath) ?? throw new Exception($"[{section.Name} section]: XPath failed for {articlesXPath}");
            foreach (var articleNode in articleNodes)
                section.Content.Add(GetContent(articleNode));
        }
        catch (Exception ex)
        {
            section.ScrapeMessage = ex.Message;
            section.ScrapeSuccess = false;
        }
        return section;
    }

    private PageSectionContent GetContent(HtmlNode contentNode)
    {
        string? unixTimestamp = contentNode.GetAttributeValue("data-updated-date-timestamp", "").Trim();
        return new()
        {
            TargetUri = new(contentNode.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "").Trim()),
            Title = contentNode.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText.Trim(),
            LastUpdatedOn = string.IsNullOrWhiteSpace(unixTimestamp) ? null : ConvertUnixTimestamp(unixTimestamp)
        };
    }

    private DateTime? ConvertUnixTimestamp(string unixTimestamp)
    {
        if (long.TryParse(unixTimestamp, out long timestamp))
        {
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
            return dateTimeOffset.UtcDateTime;
        }
        return null;
    }

    private string TrimInnerHtmlWhitespace(string html)
    {
        // Replace multiple whitespace (including newlines/tabs) with a single space
        return Regex.Replace(html, @"\s+", " ").Trim();
    }
}
