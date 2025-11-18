using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using SnapshotJob.Configuration.Options;
using SnapshotJob.Data.Models;
using SnapshotJob.Data.Repositories;
using SnapshotJob.Models;
using SnapshotJob.Scrapers.MainPage.Sections;

namespace SnapshotJob.Scrapers.MainPage;

internal class MainPageScraper(ScrapedHeadlineRepository headlineRepository, IOptions<ScrapingOptions> options)
{
    private readonly Uri _sourceUri = options.Value.BaseUri;
    private readonly bool _skipScrape = options.Value.SkipMainPageScrape;
    private readonly string _testFile = options.Value.MainPageTestFile;
    private readonly bool _useTestFile = options.Value.UseMainPageTestFile;

    public async Task<ScrapeMainPageResult> ScrapeAsync(long jobId)
    {
        ScrapeMainPageResult scrapeResult = new() 
        { 
            Source = _useTestFile ? _testFile : _sourceUri.AbsoluteUri, 
            StartedOn = DateTime.UtcNow 
        };
        if (_skipScrape)
            return await CreateTestHeadline(jobId);
        
        try
        {
            // Get the Main Page HTML or the HTML test file
            HtmlDocument htmlDocument = new();
            if (_useTestFile && !string.IsNullOrWhiteSpace(_testFile))
                htmlDocument.Load(_testFile);
            else
                htmlDocument.LoadHtml(await new HttpClient().GetStringAsync(_sourceUri));

            // Scrape the HTML document sections for Headlinesokli
            var documentNode = htmlDocument.DocumentNode;
            scrapeResult.AddScrapeSectionResult(new MainStoryScraper(documentNode, "A1").Scrape());
            scrapeResult.AddScrapeSectionResult(new MainStoryScraper(documentNode, "A2").Scrape());
            scrapeResult.AddScrapeSectionResult(new A3Scraper(documentNode).Scrape());
            scrapeResult.AddScrapeSectionResult(new CBlockScraper(documentNode).Scrape());
            scrapeResult.AddScrapeSectionResult(new B1Scraper(documentNode).Scrape());
            scrapeResult.AddScrapeSectionResult(new B2Scraper(documentNode).Scrape());
            scrapeResult.AddScrapeSectionResult(new IcymiScraper(documentNode).Scrape());
            scrapeResult.AddScrapeSectionResult(new BeWellScraper(documentNode).Scrape());
            scrapeResult.AddScrapeSectionResult(new USNewsScraper(documentNode).Scrape());
            scrapeResult.AddScrapeSectionResult(new WorldNewsScraper(documentNode).Scrape());
            scrapeResult.AddScrapeSectionResult(new PoliticsScraper(documentNode).Scrape());
            scrapeResult.AddScrapeSectionResult(new EntertainmentScraper(documentNode).Scrape());
            scrapeResult.AddScrapeSectionResult(new SportsScraper(documentNode).Scrape());
            scrapeResult.AddScrapeSectionResult(new BusinessScraper(documentNode).Scrape());
            scrapeResult.AddScrapeSectionResult(new ScienceScraper(documentNode).Scrape());
            scrapeResult.AddScrapeSectionResult(new TechnologyScraper(documentNode).Scrape());
            scrapeResult.AddScrapeSectionResult(new HealthScraper(documentNode).Scrape());
            scrapeResult.AddScrapeSectionResult(new ClimateScraper(documentNode).Scrape());
            scrapeResult.AddScrapeSectionResult(new FactCheckScraper(documentNode).Scrape());
            scrapeResult.AddScrapeSectionResult(new LatestNewsScraper(documentNode).Scrape());

            // Mark any headlines as "Most Read" that were found in the Most Read section
            var mostReadSection = new MostReadScraper(documentNode).Scrape();
            if (scrapeResult.ScrapedHeadlines is not null)
                foreach (var headline in scrapeResult.ScrapedHeadlines)
                    if (mostReadSection.Headlines.Contains(headline))
                        headline.MostRead = true;

            // Get all "article" and "live" hyperlinks on the page
            var allLinks = GetAllHyperlinks(_sourceUri, htmlDocument.DocumentNode); // TODO: Compare articles found with all links

            // Save the headlines to the database
            if (scrapeResult.ScrapedHeadlines is not null)
                foreach (var headline in scrapeResult.ScrapedHeadlines.Where(h =>
                    h.TargetUri.AbsoluteUri.StartsWith($"{_sourceUri}article/", StringComparison.OrdinalIgnoreCase)))
                {
                    try
                    {
                        // Check if the TargetUri already exists in the table
                        if (await headlineRepository.ExistsAsync(headline.TargetUri))
                        {
                            headline.AlreadyInDatabase = true;
                            continue;
                        }

                        // Save the headline
                        headline.Id = await headlineRepository.CreateAsync(headline, jobId);   
                    }
                    catch (Exception ex)
                    {
                        scrapeResult.Exceptions ??= [];
                        scrapeResult.Exceptions.Add(ex);
                    }
                }
        }
        catch (Exception ex)
        {
            scrapeResult.Exceptions ??= [];
            scrapeResult.Exceptions.Add(ex);
        }
        
        return scrapeResult;
    }
   
    public async Task<ScrapeMainPageResult> CreateTestHeadline(long jobId)
    {
        ScrapeMainPageResult result = new() 
        {
            Source = _useTestFile ? _testFile : _sourceUri.AbsoluteUri,
            StartedOn = DateTime.UtcNow 
        };
        ScrapedHeadline headline = new() { Headline = "Test Headline", TargetUri = new($"https://test.com/{DateTime.UtcNow.Ticks}") };
        headline.Id = await headlineRepository.CreateAsync(headline, jobId);
        result.ScrapedHeadlines ??= [];
        result.ScrapedHeadlines.Add(headline);
        return result;
    }

    private List<string> GetAllHyperlinks(Uri sourceUri, HtmlNode documentNode)
    {
        return documentNode.SelectNodes("//a[@href]")?.Select(node => node.GetAttributeValue("href", ""))
            .Where(href =>
                href.StartsWith($"{sourceUri}/article/", StringComparison.OrdinalIgnoreCase) ||
                href.StartsWith($"{sourceUri}/live/", StringComparison.OrdinalIgnoreCase))
            .Distinct().ToList() ?? [];
    }
}
