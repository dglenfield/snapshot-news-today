using SnapshotJob.Models;

namespace SnapshotJob.Scrapers.MainPage.Sections;

public interface IPageSectionScraper
{
    string SectionName { get; }
    ScrapeSectionResult Scrape();
}
