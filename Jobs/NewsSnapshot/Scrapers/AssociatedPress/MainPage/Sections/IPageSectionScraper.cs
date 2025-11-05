using Common.Models;

namespace NewsSnapshot.Scrapers.AssociatedPress.MainPage.Sections;

public interface IPageSectionScraper
{
    string SectionName { get; }
    ScrapeSectionResult Scrape();
}
