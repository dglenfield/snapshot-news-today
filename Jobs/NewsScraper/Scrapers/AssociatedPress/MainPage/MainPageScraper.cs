using Common.Logging;
using HtmlAgilityPack;
using NewsScraper.Models.AssociatedPress.MainPage;
using NewsScraper.Scrapers.AssociatedPress.MainPage.Sections;
using System.Text.RegularExpressions;

namespace NewsScraper.Scrapers.AssociatedPress.MainPage;

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

        // Get the Main Page HTML or the HTML test file
        HtmlDocument htmlDocument = new();
        if (_useTestFile)
            htmlDocument.Load(_testFile);
        else
            htmlDocument.LoadHtml(await new HttpClient().GetStringAsync(_baseUrl));

        // Scrape the HTML document for Page Sections and content
        scrapeResult.Sections = ProcessDocument(htmlDocument.DocumentNode);

        // Logging for testing
        logger.Log("\nScraping Exceptions:");
        foreach (var section in scrapeResult.Sections)
            if (section.ScrapeException is not null)
                logger.LogException(section.ScrapeException);
        logger.Log("\nScraping Messages:");
        foreach (var section in scrapeResult.Sections)
            if (section.ScrapeMessage is not null)
                if (section.ScrapeSuccess.HasValue && section.ScrapeSuccess.Value == false)
                    logger.Log(section.ScrapeMessage, LogLevel.Error);
                else
                    logger.Log(section.ScrapeMessage);
        
        return scrapeResult;
    }

    public List<PageSection> ProcessDocument(HtmlNode documentNode)
    {
        List<PageSection> pageSections = [];

        pageSections.Add(new MainStoryScraper(documentNode, "A1").Scrape());
        pageSections.Add(new MainStoryScraper(documentNode, "A2").Scrape());
        //pageSections.Add(new A3Scraper(documentNode).Scrape());
        //pageSections.Add(new CBlockScraper(documentNode).Scrape());
        //pageSections.Add(new MostReadScraper(documentNode).Scrape()); // TODO: Most Read can be duplicates of other articles so it's good to mark as "Most Read"
        //pageSections.Add(new B1Scraper(documentNode).Scrape());


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

        // Logging results
        if (_useTestFile)
            logger.Log($"Scraping results from test file: {_testFile}", logAsRawMessage: true);
        else
            logger.Log($"Scraping results from {_baseUrl}", logAsRawMessage: true);
        logger.Log($"{pageSections.Count} page sections found");
        int articleCount = 0;
        foreach (var section in pageSections)
        {
            logger.Log($"{section.Name} Section", logAsRawMessage: true);
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
            logger.Log($"{section.Content.Count} articles found\n", logAsRawMessage: true);
        }
        logger.Log($"Total articles found: {articleCount}", logAsRawMessage: true);
        logger.Log($"Total hyperlinks found: {allLinks.Count}", logAsRawMessage: true);

        return pageSections;
    }

    private List<string> GetAllHyperlinks(HtmlNode documentNode)
    {
        return documentNode.SelectNodes("//a[@href]")?.Select(node => node.GetAttributeValue("href", ""))
            .Where(href =>
                href.StartsWith($"{_baseUrl}/article/", StringComparison.OrdinalIgnoreCase) ||
                href.StartsWith($"{_baseUrl}/live/", StringComparison.OrdinalIgnoreCase))
            .Distinct().ToList() ?? [];
    }

   
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
