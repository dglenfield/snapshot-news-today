using Microsoft.Extensions.Options;
using SnapshotJob.Common.Logging;
using SnapshotJob.Configuration.Options;
using SnapshotJob.Data;
using SnapshotJob.Data.Models;
using SnapshotJob.Data.Repositories;
using SnapshotJob.Perplexity;
using SnapshotJob.Perplexity.Models.TopStories;
using SnapshotJob.Scrapers.Models;
using System.Text.Json;

namespace SnapshotJob.Processors;

internal class SnapshotJobProcessor(ScrapeProcessor scrapeProcessor, TopStoriesProcessor topStoriesProcessor,
    NewsSnapshotRepository newsSnapshotRepository, ScrapedArticleRepository scrapedArticleRepository,
    Logger logger, IOptions<ApplicationOptions> options,
    SnapshotJobDatabase database, ArticleProvider articleProvider, AnalyzedArticleRepository analyzedArticleRepository)
{
    private readonly NewsSnapshot _snapshot = new();

    internal async Task Run()
    {
        _snapshot.StartedOn = DateTime.UtcNow;

        ScrapeMainPageResult? scrapeMainPageResult = null;
        ScrapeArticlesResult? scrapeArticlesResult = null;
        TopStoriesResult? topStoriesResult = null;

        try
		{
            // Insert initial News Snapshot record to track this session
            _snapshot.Id = await newsSnapshotRepository.CreateAsync(_snapshot.StartedOn.Value);

            // Scrape the main page for headlines
            if (!options.Value.SkipMainPageScrape)
                scrapeMainPageResult = await ScrapeMainPage();

            // Scrape the article for each headline
            if (!options.Value.SkipArticleScrape && scrapeMainPageResult is not null)
                scrapeArticlesResult = await ScrapeArticles(scrapeMainPageResult);

            // Get the Top Stories for the scraped headlines
            if (!options.Value.SkipTopStories)
            {
                if (scrapeArticlesResult?.ScrapedArticles is null)
                {
                    var scrapedArticles = await scrapedArticleRepository.GetBySnapshotId(1);
                    scrapeArticlesResult = new() { ScrapedArticles = scrapedArticles };
                }

                topStoriesResult = await topStoriesProcessor.SelectStories(scrapeArticlesResult.ScrapedArticles, _snapshot.Id);
                if (topStoriesResult?.TopStories is not null)
                {
                    foreach (NewsStory story in topStoriesResult.TopStories)
                    {
                        if (long.TryParse(story.Id, out long scrapedArticleId))
                        {
                            ScrapedArticle? scrapedArticle = await scrapedArticleRepository.GetByIdAsync(scrapedArticleId);
                            if (scrapedArticle is null)
                                continue;

                            // Analyze the article with Perplexity API
                            var analyzeArticleResult = await articleProvider.Analyze(scrapedArticle);
                            logger.Log("\n" + analyzeArticleResult);

                            // Save the analyzed article to the database
                            if (analyzeArticleResult.Content is null)
                                continue;

                            AnalyzedArticle analyzedArticle = new()
                            {
                                AnalyzedOn = DateTime.UtcNow,
                                CustomHeadline = analyzeArticleResult.Content.CustomHeadline,
                                Exception = analyzeArticleResult.Exception,
                                KeyPoints = analyzeArticleResult.Content.KeyPoints,
                                KeyPointsJson = JsonSerializer.Serialize(analyzeArticleResult.Content.KeyPoints),
                                ScrapedArticleId = scrapedArticle.Id,
                                Summary = analyzeArticleResult.Content.Summary
                            };

                            await analyzedArticleRepository.CreateAsync(analyzedArticle);

                            break;
                        }
                            
                    }
                }
            }

            // Publish analyzed articles for top stories
            // Save to Cosmos DB

            _snapshot.IsSuccess = true;
        }
		catch (Exception ex)
		{
            _snapshot.IsSuccess = false;
            _snapshot.SnapshotException = ex;
            throw;
        }
        finally
        {
            // Update job record with results
            _snapshot.FinishedOn = DateTime.UtcNow;
            await newsSnapshotRepository.UpdateAsync(_snapshot);

            // Log the results
            //WriteToLog(scrapeMainPageResult, scrapeArticlesResult, topStoryArticles);
            logger.Log($"\nNews snapshot job finished {(_snapshot.IsSuccess!.Value ? "successfully" : "unsuccessfully")}.",
                messageLogLevel: (_snapshot.IsSuccess!.Value ? LogLevel.Success : LogLevel.Error));
        }
    }

    private async Task<ScrapeArticlesResult> ScrapeArticles(ScrapeMainPageResult scrapeMainPageResult)
    {
        if (scrapeMainPageResult.Headlines is null)
            throw new ArgumentNullException("scrapeMainPageResult.Headlines");

        var scrapeArticlesResult = await scrapeProcessor.ScrapeArticles([.. scrapeMainPageResult.Headlines]);
        _snapshot.ArticlesScraped = scrapeArticlesResult.ArticlesScraped;

        if (scrapeArticlesResult.ScrapedArticles is not null)
        {
            foreach (var article in scrapeArticlesResult.ScrapedArticles.Where(a => a.Exceptions is not null))
            {
                _snapshot.ScrapeExceptions ??= [];
                _snapshot.ScrapeExceptions.AddRange(article.Exceptions!);
            }
        }

        return scrapeArticlesResult;
    }

    private async Task<ScrapeMainPageResult> ScrapeMainPage()
    {
        var scrapeMainPageResult = await scrapeProcessor.ScrapeMainPage(_snapshot.Id);
        _snapshot.SectionsScraped = scrapeMainPageResult.SectionsScraped;
        _snapshot.HeadlinesScraped = scrapeMainPageResult.HeadlinesScraped;
        
        if (scrapeMainPageResult.Exceptions is not null)
        {
            _snapshot.ScrapeExceptions ??= [];
            _snapshot.ScrapeExceptions.AddRange(scrapeMainPageResult.Exceptions);
        }

        return scrapeMainPageResult;
    }

    private void WriteToLog(ScrapeMainPageResult? scrapeMainPageResult, ScrapeArticlesResult? scrapeArticlesResult, 
        TopStoriesResult? topStoryArticles)
    {
        bool testFileUsed = Uri.TryCreate(scrapeMainPageResult?.Source, UriKind.Absolute, out Uri? sourceUri);
        if (testFileUsed)
            logger.Log($"\nScraping results from test file: {scrapeMainPageResult?.Source}", consoleColor: ConsoleColor.Yellow);
        else
            logger.Log($"\nScraping results from {sourceUri?.AbsoluteUri}", consoleColor: ConsoleColor.Blue);

        // News Snapshot Exception
        if (_snapshot?.SnapshotException is not null)
        {
            logger.Log($"\nNews Snapshot Exception in {_snapshot.SnapshotException.Source}", LogLevel.Error, logAsRawMessage: true);
            logger.LogException(_snapshot.SnapshotException);
        }

        // Scrape Headlines Exceptions
        if (scrapeMainPageResult?.Exceptions is not null)
        {
            var exceptions = scrapeMainPageResult.Exceptions;
            logger.Log($"\nScrape Headlines Exceptions: {(exceptions?.Count > 0 ? exceptions.Count : "None")}",
                consoleColor: exceptions?.Count > 0 ? ConsoleColor.DarkRed : ConsoleColor.DarkGreen, logAsRawMessage: true);
            int scrapeExceptionCount = 0;
            if (exceptions is not null)
            {
                foreach (var exception in exceptions)
                {
                    logger.Log($"\n{++scrapeExceptionCount}: Exception in {exception.Source}", LogLevel.Error, logAsRawMessage: true);
                    logger.LogException(exception);
                }
            }
        }

        // Scrape Articles Exceptions
        var articlesWithException = scrapeArticlesResult?.ScrapedArticles?.Where(a => a.Exceptions is not null);
        logger.Log($"\nArticles with Exceptions: {(articlesWithException is null ? "None" : articlesWithException.Count())}",
            consoleColor: articlesWithException is null ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed, logAsRawMessage: true);
        if (articlesWithException is not null)
        {
            int exceptionCount = 0;
            foreach (var article in articlesWithException)
            {
                foreach (var exception in article.Exceptions!)
                {
                    logger.Log($"\n{++exceptionCount}: Exception in {exception.Source}", LogLevel.Error, logAsRawMessage: true);
                    logger.LogException(exception);
                }
            }
        }

        // Page Sections and Headlines
        logger.Log($"\n{scrapeMainPageResult?.SectionsScraped} page sections found", logAsRawMessage: true,
            consoleColor: scrapeMainPageResult?.SectionsScraped > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed);
        if (scrapeMainPageResult?.Headlines is not null)
        {
            int headlineCount = 0;
            foreach (var section in scrapeMainPageResult.Headlines.DistinctBy(a => a.SectionName))
            {
                string sectionName = $"{(string.IsNullOrWhiteSpace(section.SectionName) ? "No" : section.SectionName)} Section";
                logger.Log($"Section Name: {sectionName}", logAsRawMessage: true, consoleColor: ConsoleColor.DarkCyan);
                var headlines = scrapeMainPageResult.Headlines.Where(a => a.SectionName == section.SectionName);
                foreach (var headline in headlines)
                {
                    logger.Log($"Headline {++headlineCount}:", logAsRawMessage: true, consoleColor: ConsoleColor.DarkYellow);
                    logger.Log(headline.ToString(), logAsRawMessage: true, consoleColor: ConsoleColor.Cyan);
                }
                logger.Log($"{headlines.Count()} headlines found in {sectionName}\n", logAsRawMessage: true,
                    consoleColor: scrapeMainPageResult.SectionsScraped > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed);
            }
        }

        // Articles
        logger.Log($"{scrapeArticlesResult?.ArticlesScraped} articles found", logAsRawMessage: true,
            consoleColor: scrapeArticlesResult?.ArticlesScraped > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed);
        if (scrapeArticlesResult?.ScrapedArticles is not null)
        {
            int articleCount = 0;
            foreach (var article in scrapeArticlesResult.ScrapedArticles)
            {
                logger.Log($"Article {++articleCount}:", logAsRawMessage: true, consoleColor: ConsoleColor.DarkYellow);
                logger.Log(article.ToString(), logAsRawMessage: true, consoleColor: (article.IsSuccess ? ConsoleColor.Cyan : ConsoleColor.DarkRed));
            }
        }

        logger.Log($"Sections scraped: {scrapeMainPageResult?.SectionsScraped}", logAsRawMessage: true,
            consoleColor: scrapeMainPageResult?.SectionsScraped > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed);
        logger.Log($"Headlines scraped: {scrapeMainPageResult?.HeadlinesScraped}", logAsRawMessage: true,
            consoleColor: scrapeMainPageResult?.HeadlinesScraped > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed);
        logger.Log($"Total Articles scraped: {scrapeArticlesResult?.ArticlesScraped}", logAsRawMessage: true,
            consoleColor: scrapeArticlesResult?.ArticlesScraped > 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed);

        // Top Stories
        if (topStoryArticles?.TopStories is not null)
        {
            int articleCount = 0;
            foreach (var article in topStoryArticles.TopStories)
            {
                logger.Log($"Article {++articleCount}:", logAsRawMessage: true);
                logger.Log(article.ToString(), logAsRawMessage: true);
            }
        }

        if (_snapshot is not null && _snapshot.FinishedOn.HasValue)
            logger.Log($"Job took {_snapshot.RunTimeInSeconds} seconds", logAsRawMessage: true, consoleColor: ConsoleColor.Yellow);

        //logger.Log($"\n{ConsoleColor.Blue}", logAsRawMessage: true, consoleColor: ConsoleColor.Blue);
        //logger.Log($"{ConsoleColor.DarkBlue}", logAsRawMessage: true, consoleColor: ConsoleColor.DarkBlue);
        //logger.Log($"{ConsoleColor.Cyan}", logAsRawMessage: true, consoleColor: ConsoleColor.Cyan);
        //logger.Log($"{ConsoleColor.DarkCyan}", logAsRawMessage: true, consoleColor: ConsoleColor.DarkCyan);
        //logger.Log($"{ConsoleColor.Gray}", logAsRawMessage: true, consoleColor: ConsoleColor.Gray);
        //logger.Log($"{ConsoleColor.DarkGray}", logAsRawMessage: true, consoleColor: ConsoleColor.DarkGray);
        //logger.Log($"{ConsoleColor.Green}", logAsRawMessage: true, consoleColor: ConsoleColor.Green);
        //logger.Log($"{ConsoleColor.DarkGreen}", logAsRawMessage: true, consoleColor: ConsoleColor.DarkGreen);
        //logger.Log($"{ConsoleColor.Magenta}", logAsRawMessage: true, consoleColor: ConsoleColor.Magenta);
        //logger.Log($"{ConsoleColor.DarkMagenta}", logAsRawMessage: true, consoleColor: ConsoleColor.DarkMagenta);
        //logger.Log($"{ConsoleColor.Red}", logAsRawMessage: true, consoleColor: ConsoleColor.Red);
        //logger.Log($"{ConsoleColor.DarkRed}", logAsRawMessage: true, consoleColor: ConsoleColor.DarkRed);
        //logger.Log($"{ConsoleColor.Yellow}", logAsRawMessage: true, consoleColor: ConsoleColor.Yellow);
        //logger.Log($"{ConsoleColor.DarkYellow}", logAsRawMessage: true, consoleColor: ConsoleColor.DarkYellow);
    }
}
