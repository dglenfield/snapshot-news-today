using NewsScraper.Models.AssociatedPress.MainPage;

namespace NewsScraper.Scrapers.AssociatedPress.MainPage.Sections;

public interface IPageSectionScraper
{
    string SectionName { get; }
    SectionScrapeResult Scrape();
}
