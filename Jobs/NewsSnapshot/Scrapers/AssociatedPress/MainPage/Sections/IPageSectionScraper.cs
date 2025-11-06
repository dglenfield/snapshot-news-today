using Common.Models.Scraping.Results;

namespace NewsSnapshot.Scrapers.AssociatedPress.MainPage.Sections;

public interface IPageSectionScraper
{
    string SectionName { get; }
    ScrapeSectionResult Scrape();
}
