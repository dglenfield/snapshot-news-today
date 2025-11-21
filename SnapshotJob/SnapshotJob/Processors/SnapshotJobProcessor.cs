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
    ArticleProvider articleProvider, SnapshotJobDatabase database, IOptions<ApplicationOptions> options, 
    Logger logger)
{
    // This news snapshot instance
    private readonly NewsSnapshot _snapshot = new();

    // Repositories
    private readonly AnalyzedArticleRepository _analyzedArticleRepository = new(database);
    private readonly NewsSnapshotRepository _newsSnapshotRepository = new(database);
    private readonly NewsSnapshotArticleRepository _newsSnapshotArticleRepository = new(database);
    private readonly ScrapedArticleRepository _scrapedArticleRepository = new(database);
    private readonly PerplexityApiCallRepository _perplexityApiCallRepository = new(database);

    internal async Task Run()
    {
        _snapshot.StartedOn = DateTime.UtcNow;

        ScrapeMainPageResult? scrapeMainPageResult = null;
        ScrapeArticlesResult? scrapeArticlesResult = null;
        List<AnalyzedArticle>? analyzedArticles = null;

        try
		{
            // Insert initial News Snapshot record to track this session
            _snapshot.Id = await _newsSnapshotRepository.CreateAsync(_snapshot.StartedOn.Value);

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
                    var scrapedArticles = await _scrapedArticleRepository.GetBySnapshotId(1);
                    scrapeArticlesResult = new() { ScrapedArticles = scrapedArticles };
                }

                TopStoriesResult? topStoriesResult = await topStoriesProcessor.SelectStories(scrapeArticlesResult.ScrapedArticles, _snapshot.Id);
                if (topStoriesResult?.TopStories is not null)
                {
                    foreach (NewsStory story in topStoriesResult.TopStories)
                    {
                        if (long.TryParse(story.Id, out long scrapedArticleId))
                        {
                            ScrapedArticle? scrapedArticle = await _scrapedArticleRepository.GetByIdAsync(scrapedArticleId);
                            if (scrapedArticle is null)
                                continue;

                            // Analyze the article with Perplexity API
                            var analyzeArticleResult = await articleProvider.Analyze(scrapedArticle);

                            // Save API call to the database
                            var usage = analyzeArticleResult.PerplexityApiUsage;
                            PerplexityApiCall apiCall = new()
                            {
                                CompletionTokens = usage is null ? 0 : usage.CompletionTokens,
                                PromptTokens = usage is null ? 0 : usage.PromptTokens,
                                TotalTokens = usage is null ? 0 : usage.TotalTokens,
                                InputTokensCost = usage is null ? 0 : usage.Cost.InputTokensCost,
                                OutputTokensCost = usage is null ? 0 : usage.Cost.OutputTokensCost,
                                RequestCost = usage is null ? 0 : usage.Cost.RequestCost,
                                TotalCost = usage is null ? 0 : usage.Cost.TotalCost,
                                RequestBody = analyzeArticleResult.RequestBody,
                                ResponseString = analyzeArticleResult.ResponseString,
                                Exception = analyzeArticleResult.Exception
                            };
                            await _perplexityApiCallRepository.CreateAsync(apiCall, _snapshot.Id);

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

                            analyzedArticle.Id = await _analyzedArticleRepository.CreateAsync(analyzedArticle);

                            analyzedArticles ??= [];
                            analyzedArticles.Add(analyzedArticle);

                            //break;
                        }
                            
                    }
                }
            }

            // Save News Snapshot Articles to database in preparation for publishing
            if (analyzedArticles is not null)
            {
                foreach (var analyzedArticle in analyzedArticles)
                {
                    // Get the scraped article data from the database
                    var scrapedArticle = await _scrapedArticleRepository.GetByIdAsync(analyzedArticle.ScrapedArticleId);
                    if (scrapedArticle is null)
                    {
                        logger.Log($"ScrapedArticleId {analyzedArticle.ScrapedArticleId} not found.", LogLevel.Warning);
                        continue;
                    }

                    NewsSnapshotArticle snapshotArticle = new() 
                    { 
                        AnalyzedArticleId = analyzedArticle.Id,
                        Author = scrapedArticle.Author,
                        ContentParagraphs = scrapedArticle.ContentParagraphs,
                        CustomHeadline = analyzedArticle.CustomHeadline,
                        KeyPoints = analyzedArticle.KeyPoints, 
                        KeyPointsJson = analyzedArticle.KeyPointsJson,
                        LastUpdatedOn = scrapedArticle.LastUpdatedOn,
                        NewsSnapshotId = _snapshot.Id,
                        SourceHeadline = scrapedArticle.Headline,
                        SourceSectionName = scrapedArticle.SectionName,
                        SourceUri = scrapedArticle.SourceUri,
                        Summary = analyzedArticle.Summary
                    };

                    try
                    {
                        if (await _newsSnapshotArticleRepository.ExistsAsync(snapshotArticle.SourceUri))
                            logger.Log($"Article already exists in news_snapshot_article", LogLevel.Warning);
                        else
                            await _newsSnapshotArticleRepository.CreateAsync(snapshotArticle);
                    }
                    catch (Exception ex)
                    {
                        _snapshot.SnapshotExceptions ??= [];
                        _snapshot.SnapshotExceptions.Add(ex);
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
            _snapshot.SnapshotExceptions ??= [];
            _snapshot.SnapshotExceptions.Add(ex);
            throw;
        }
        finally
        {
            // Update job record with results
            _snapshot.FinishedOn = DateTime.UtcNow;
            await _newsSnapshotRepository.UpdateAsync(_snapshot);

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
        if (_snapshot?.SnapshotExceptions is not null)
        {
            foreach (var exception in _snapshot.SnapshotExceptions)
            {
                logger.Log($"\nException in {exception.Source}", LogLevel.Error, logAsRawMessage: true);
                logger.LogException(exception);
            }
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
    }
}
