using HtmlAgilityPack;
using NewsScraper.Data.Repositories;
using NewsScraper.Models.AssociatedPress;
using NewsScraper.Models.AssociatedPress.MainPage;
using NewsScraper.Scrapers.AssociatedPress.MainPage.Sections;

namespace NewsScraper.Scrapers.AssociatedPress.MainPage;

internal class MainPageScraper(AssociatedPressHeadlineRepository headlineRepository)
{
    public async Task<ScrapeMainPageResult> ScrapeAsync(ScrapeJob job)
    {
        ScrapeMainPageResult scrapeResult = new() { ScrapedOn = DateTime.UtcNow };
        try
        {
            // Get the Main Page HTML or the HTML test file
            HtmlDocument htmlDocument = new();
            if (job.UseMainPageTestFile && !string.IsNullOrWhiteSpace(job.MainPageTestFile))
                htmlDocument.Load(job.MainPageTestFile);
            else
                htmlDocument.LoadHtml(await new HttpClient().GetStringAsync(job.SourceUri));

            // Scrape the HTML document sections for Headlines
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
            foreach (var headline in scrapeResult.Headlines)
                if (mostReadSection.Headlines.Contains(headline))
                    headline.MostRead = true;

            // Get all "article" and "live" hyperlinks on the page
            var allLinks = GetAllHyperlinks(job.SourceUri, htmlDocument.DocumentNode); // TODO: Compare articles found with all links

            // Save the headlines to the database
            foreach (var headline in scrapeResult.Headlines)
            {
                try
                {
                    headline.Id = await headlineRepository.CreateAsync(headline, job.Id);
                }
                catch (Exception ex)
                {
                    scrapeResult.ScrapeExceptions.Add(new() { Source = $"Saving headline with TargetUri of {headline.TargetUri}", Exception = ex });
                }
            }
        }
        catch (Exception ex)
        {
            scrapeResult.ScrapeExceptions.Add(new() { Source = $"{nameof(ScrapeAsync)}", Exception = ex });
        }

        return scrapeResult;
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
