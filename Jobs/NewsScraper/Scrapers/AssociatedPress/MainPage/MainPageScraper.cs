using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using NewsScraper.Configuration.Options;
using NewsScraper.Models.AssociatedPress.MainPage;
using NewsScraper.Scrapers.AssociatedPress.MainPage.Sections;
using System.Text.RegularExpressions;

namespace NewsScraper.Scrapers.AssociatedPress.MainPage;

internal class MainPageScraper(IOptions<NewsSourceOptions> newsSourceOptions)
{
    private readonly Uri _baseUri = newsSourceOptions.Value.AssociatedPress.BaseUri;
    private readonly string _testFile = newsSourceOptions.Value.AssociatedPress.Scrapers.MainPage.TestFile;
    private readonly bool _useTestFile = newsSourceOptions.Value.AssociatedPress.Scrapers.MainPage.UseTestFile;

    public async Task<ScrapeResult> Scrape()
    {
        ScrapeResult scrapeResult = new()
        {
            SourceUri = _baseUri,
            ScrapedOn = DateTime.UtcNow
        };

        // Get the Main Page HTML or the HTML test file
        HtmlDocument htmlDocument = new();
        if (_useTestFile)
            htmlDocument.Load(_testFile);
        else
            htmlDocument.LoadHtml(await new HttpClient().GetStringAsync(_baseUri));

        // Scrape the HTML document for Page Sections and content
        scrapeResult.Sections = ProcessDocument(htmlDocument.DocumentNode);

        // Get all "article" and "live" hyperlinks on the page
        var allLinks = GetAllHyperlinks(htmlDocument.DocumentNode); // TODO: Compare articles found with all links
        
        return scrapeResult;
    }

    private List<PageSection> ProcessDocument(HtmlNode documentNode)
    {
        List<PageSection> pageSections = [];
        pageSections.Add(new MainStoryScraper(documentNode, "A1").Scrape());
        pageSections.Add(new MainStoryScraper(documentNode, "A2").Scrape());
        pageSections.Add(new A3Scraper(documentNode).Scrape());
        pageSections.Add(new CBlockScraper(documentNode).Scrape());
        pageSections.Add(new B1Scraper(documentNode).Scrape());
        pageSections.Add(new B2Scraper(documentNode).Scrape());
        pageSections.Add(new IcymiScraper(documentNode).Scrape());
        pageSections.Add(new BeWellScraper(documentNode).Scrape());
        pageSections.Add(new USNewsScraper(documentNode).Scrape());
        pageSections.Add(new WorldNewsScraper(documentNode).Scrape());
        pageSections.Add(new PoliticsScraper(documentNode).Scrape());
        pageSections.Add(new EntertainmentScraper(documentNode).Scrape());
        pageSections.Add(new SportsScraper(documentNode).Scrape());
        pageSections.Add(new BusinessScraper(documentNode).Scrape());
        pageSections.Add(new ScienceScraper(documentNode).Scrape());
        pageSections.Add(new TechnologyScraper(documentNode).Scrape());
        pageSections.Add(new HealthScraper(documentNode).Scrape());
        pageSections.Add(new ClimateScraper(documentNode).Scrape());
        pageSections.Add(new FactCheckScraper(documentNode).Scrape());
        pageSections.Add(new LatestNewsScraper(documentNode).Scrape());

        // Mark any articles as "Most Read" that were found in the Most Read section
        var mostReadSection = new MostReadScraper(documentNode).Scrape();
        foreach (var section in pageSections)
            foreach (var article in section.Content)
                if (mostReadSection.Content.Contains(article))
                    article.MostRead = true;

        return pageSections;
    }

    private List<string> GetAllHyperlinks(HtmlNode documentNode)
    {
        return documentNode.SelectNodes("//a[@href]")?.Select(node => node.GetAttributeValue("href", ""))
            .Where(href =>
                href.StartsWith($"{_baseUri}/article/", StringComparison.OrdinalIgnoreCase) ||
                href.StartsWith($"{_baseUri}/live/", StringComparison.OrdinalIgnoreCase))
            .Distinct().ToList() ?? [];
    }

    private string TrimInnerHtmlWhitespace(string html)
    {
        // Replace multiple whitespace (including newlines/tabs) with a single space
        return Regex.Replace(html, @"\s+", " ").Trim();
    }
}
