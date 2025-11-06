using SnapshotJob.Models;

namespace SnapshotJob.Scrapers.AssociatedPress.MainPage.Sections;

public interface IPageSectionScraper
{
    string SectionName { get; }
    ScrapeSectionResult Scrape();
}
