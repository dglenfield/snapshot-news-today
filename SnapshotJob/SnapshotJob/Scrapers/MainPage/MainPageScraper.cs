using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using SnapshotJob.Configuration.Options;
using SnapshotJob.Data.Models;
using SnapshotJob.Data.Repositories;
using SnapshotJob.Scrapers.MainPage.Sections;
using SnapshotJob.Scrapers.Models;

namespace SnapshotJob.Scrapers.MainPage;

internal class MainPageScraper
{
    private readonly ScrapingOptions _options;
    private readonly ScrapedHeadlineRepository _repository;
    private readonly ScrapeMainPageResult _scrapeResult;

    public MainPageScraper(ScrapedHeadlineRepository repository, IOptions<ScrapingOptions> options)
    {
        _options = options.Value;
        _repository = repository;
        _scrapeResult = new() { Source = _options.UseMainPageTestFile ? _options.MainPageTestFile : _options.BaseUri.AbsoluteUri };
    }

    public async Task<ScrapeMainPageResult> ScrapeAsync(long jobId)
    {   
        HtmlDocument htmlDocument = new();
        try
        {
            // Get the Main Page HTML or the HTML test file
            _scrapeResult.StartedOn = DateTime.UtcNow;
            if (_options.UseMainPageTestFile && !string.IsNullOrWhiteSpace(_options.MainPageTestFile))
                htmlDocument.Load(_options.MainPageTestFile);
            else
                htmlDocument.LoadHtml(await new HttpClient().GetStringAsync(_options.BaseUri));
        }
        catch (Exception ex)
        {
            _scrapeResult.Exceptions ??= [];
            _scrapeResult.Exceptions.Add(ex);
            return _scrapeResult;
        }

        // Scrape the HTML document sections for Headlines
        HtmlNode documentNode = htmlDocument.DocumentNode;
        ScrapeSection(new MainStoryScraper(documentNode, "A1"));
        ScrapeSection(new MainStoryScraper(documentNode, "A2"));
        ScrapeSection(new A3Scraper(documentNode));
        ScrapeSection(new CBlockScraper(documentNode));
        ScrapeSection(new B1Scraper(documentNode));
        ScrapeSection(new B2Scraper(documentNode));
        ScrapeSection(new IcymiScraper(documentNode));
        ScrapeSection(new BeWellScraper(documentNode));
        ScrapeSection(new USNewsScraper(documentNode));
        ScrapeSection(new WorldNewsScraper(documentNode));
        ScrapeSection(new PoliticsScraper(documentNode));
        ScrapeSection(new EntertainmentScraper(documentNode));
        ScrapeSection(new SportsScraper(documentNode));
        ScrapeSection(new BusinessScraper(documentNode));
        ScrapeSection(new ScienceScraper(documentNode));
        ScrapeSection(new TechnologyScraper(documentNode));
        ScrapeSection(new HealthScraper(documentNode));
        ScrapeSection(new ClimateScraper(documentNode));
        ScrapeSection(new FactCheckScraper(documentNode));
        ScrapeSection(new LatestNewsScraper(documentNode));

        try
        {
            // Mark any headlines as "Most Read" that were found in the Most Read section
            var mostReadHeadlines = new MostReadScraper(documentNode).Scrape();
            if (_scrapeResult.Headlines is not null)
                foreach (var headline in _scrapeResult.Headlines)
                    if (mostReadHeadlines.Contains(headline))
                        headline.MostRead = true;
        }
        catch (Exception ex)
        {
            _scrapeResult.Exceptions ??= [];
            _scrapeResult.Exceptions.Add(ex);
        }

        if (_scrapeResult.Headlines is null)
            return _scrapeResult;

        // Save the headlines to the database
        foreach (var headline in _scrapeResult.Headlines.Where(h =>
            h.TargetUri.AbsoluteUri.StartsWith($"{_options.BaseUri}article/", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                // Check if the TargetUri already exists in the table
                if (await _repository.ExistsAsync(headline.TargetUri))
                {
                    headline.AlreadyInDatabase = true;
                    continue;
                }

                // Save the headline
                headline.Id = await _repository.CreateAsync(headline, jobId);   
            }
            catch (Exception ex)
            {
                _scrapeResult.Exceptions ??= [];
                _scrapeResult.Exceptions.Add(ex);
            }
        }
        
        return _scrapeResult;
    }
   
    private async Task<ScrapeMainPageResult> CreateTestHeadline(long jobId)
    {
        try
        {
            _scrapeResult.StartedOn = DateTime.UtcNow;
            ScrapedHeadline headline = new() { Headline = "Test Headline", TargetUri = new($"https://test.com/{DateTime.UtcNow.Ticks}") };
            headline.Id = await _repository.CreateAsync(headline, jobId);
            _scrapeResult.Headlines ??= [];
            _scrapeResult.Headlines.Add(headline);
        }
        catch (Exception ex)
        {
            _scrapeResult.Exceptions ??= [];
            _scrapeResult.Exceptions.Add(ex);
        }
        
        return _scrapeResult;
    }

    private void ScrapeSection(PageSectionScraperBase sectionScraper)
    {
        try
        {
            foreach (var headline in sectionScraper.Scrape())
            {
                _scrapeResult.Headlines ??= [];
                _scrapeResult.Headlines.Add(headline);
            }
        }
        catch (NodeNotFoundException ex)
        {
            _scrapeResult.Exceptions ??= [];
            _scrapeResult.Exceptions.Add(ex);
        }
        catch (Exception ex)
        {
            _scrapeResult.Exceptions ??= [];
            _scrapeResult.Exceptions.Add(ex);
        }
    }
}
