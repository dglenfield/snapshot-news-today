using HtmlAgilityPack;
using NewsScraper.Models.AssociatedPress.MainPage;
using NewsScraper.Scrapers.AssociatedPress.MainPage.Sections;

namespace NewsScraper.Scrapers.AssociatedPress.MainPage;

internal class MainPageScraper
{
    public async Task<PageScrapeResult> Scrape(Uri sourceUri, bool useTestFile = false, string? testFile = null)
    {
        PageScrapeResult scrapeResult = new() { ScrapedOn = DateTime.UtcNow };
        try
        {
            // Get the Main Page HTML or the HTML test file
            HtmlDocument htmlDocument = new();
            if (useTestFile && !string.IsNullOrWhiteSpace(testFile))
                htmlDocument.Load(testFile);
            else
                htmlDocument.LoadHtml(await new HttpClient().GetStringAsync(sourceUri));

            // Scrape the HTML document sections for Headlines
            var documentNode = htmlDocument.DocumentNode;
            scrapeResult.AddSectionScrapeResult(new MainStoryScraper(documentNode, "A1").Scrape());
            scrapeResult.AddSectionScrapeResult(new MainStoryScraper(documentNode, "A2").Scrape());
            scrapeResult.AddSectionScrapeResult(new A3Scraper(documentNode).Scrape());
            scrapeResult.AddSectionScrapeResult(new CBlockScraper(documentNode).Scrape());
            scrapeResult.AddSectionScrapeResult(new B1Scraper(documentNode).Scrape());
            scrapeResult.AddSectionScrapeResult(new B2Scraper(documentNode).Scrape());
            scrapeResult.AddSectionScrapeResult(new IcymiScraper(documentNode).Scrape());
            scrapeResult.AddSectionScrapeResult(new BeWellScraper(documentNode).Scrape());
            scrapeResult.AddSectionScrapeResult(new USNewsScraper(documentNode).Scrape());
            scrapeResult.AddSectionScrapeResult(new WorldNewsScraper(documentNode).Scrape());
            scrapeResult.AddSectionScrapeResult(new PoliticsScraper(documentNode).Scrape());
            scrapeResult.AddSectionScrapeResult(new EntertainmentScraper(documentNode).Scrape());
            scrapeResult.AddSectionScrapeResult(new SportsScraper(documentNode).Scrape());
            scrapeResult.AddSectionScrapeResult(new BusinessScraper(documentNode).Scrape());
            scrapeResult.AddSectionScrapeResult(new ScienceScraper(documentNode).Scrape());
            scrapeResult.AddSectionScrapeResult(new TechnologyScraper(documentNode).Scrape());
            scrapeResult.AddSectionScrapeResult(new HealthScraper(documentNode).Scrape());
            scrapeResult.AddSectionScrapeResult(new ClimateScraper(documentNode).Scrape());
            scrapeResult.AddSectionScrapeResult(new FactCheckScraper(documentNode).Scrape());
            scrapeResult.AddSectionScrapeResult(new LatestNewsScraper(documentNode).Scrape());

            // Mark any headlines as "Most Read" that were found in the Most Read section
            var mostReadSection = new MostReadScraper(documentNode).Scrape();
            foreach (var headline in scrapeResult.Headlines)
                if (mostReadSection.Headlines.Contains(headline))
                    headline.MostRead = true;

            // Get all "article" and "live" hyperlinks on the page
            var allLinks = GetAllHyperlinks(sourceUri, htmlDocument.DocumentNode); // TODO: Compare articles found with all links
        }
        catch (Exception ex)
        {
            scrapeResult.ScrapeExceptions.Add(new() { Source = $"{nameof(Scrape)}", Exception = ex });
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
