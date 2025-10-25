using Common.Logging;
using HtmlAgilityPack;
using NewsScraper.Data.Repositories;
using NewsScraper.Models.AssociatedPress.ArticlePage;
using NewsScraper.Models.AssociatedPress.MainPage;

using System.Text.RegularExpressions;

namespace NewsScraper.Scrapers.AssociatedPress.ArticlePage;

internal class ArticlePageScraper(AssociatedPressArticleRepository articleRepository, Logger logger)
{
    public async Task<Article> ScrapeAsync(Headline headline, bool useTestFile = false, string? testFile = null)
    {
        Article article = new()
        {
            HeadlineId = headline.Id,
            ScrapedOn = DateTime.UtcNow
        };
        if (useTestFile == false)
            article.SourceUri = headline.TargetUri;
        else
            article.TestFile = testFile;

        try
        {
            article.Id = await articleRepository.CreateAsync(article);

            // Get the Main Page HTML or the HTML test file
            HtmlDocument htmlDocument = new();
            if (useTestFile && !string.IsNullOrWhiteSpace(testFile))
                htmlDocument.Load(testFile);
            else
                htmlDocument.LoadHtml(await new HttpClient().GetStringAsync(article.SourceUri));

            var headlineNode = htmlDocument.DocumentNode.SelectSingleNode("//h1");
            var authorNode = htmlDocument.DocumentNode.SelectSingleNode("//span[contains(@class, 'Component-headlineBylineAuthor')]");
            var publishDateNode = htmlDocument.DocumentNode.SelectSingleNode("//span[contains(@class, 'Component-headlineBylineDate')]");
            var contentNodes = htmlDocument.DocumentNode.SelectNodes("//p");
            string articleHeadline = headlineNode?.InnerText.Trim() ?? "N/A";
            string author = authorNode?.InnerText.Trim() ?? "N/A";
            string publishDate = publishDateNode?.InnerText.Trim() ?? "N/A";
            List<string> contentParagraphs = contentNodes?.Select(p => p.InnerText.Trim()).ToList() ?? new List<string>();
            logger.Log($"Headline: {articleHeadline}", LogLevel.Info);
            logger.Log($"Author: {author}", LogLevel.Info);
            logger.Log($"Publish Date: {publishDate}", LogLevel.Info);
            logger.Log("Content Paragraphs:", LogLevel.Info);
            foreach (var paragraph in contentParagraphs)
                logger.Log(paragraph, LogLevel.Info);

            var modifiedDate = ExtractUnixTimestamp(htmlDocument);
            if (modifiedDate.HasValue)
            {
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(modifiedDate.Value);
                DateTime localDateTime = dateTimeOffset.LocalDateTime;
                logger.Log($"Modified Date (Local): {localDateTime}", LogLevel.Info);
            }
            else
                logger.Log($"Modified Date: {modifiedDate}", LogLevel.Info);

            article.Headline = articleHeadline;
            article.PublishedOn = DateTime.TryParse(publishDate, out var pubDate) ? pubDate : null;
            article.Author = author;
            article.ContentParagraphs = contentParagraphs;

            await articleRepository.UpdateAsync(article);
        }
        catch (Exception ex)
        {
            article.ScrapeException = new ScrapeException() { Source = $"{nameof(ArticlePageScraper)}.{nameof(ScrapeAsync)}", Exception = ex };
        }

        return article;
    }

    private long? ExtractUnixTimestamp(HtmlDocument htmlDocument)
    {
        var bspNode = htmlDocument.DocumentNode.SelectSingleNode("//bsp-timestamp[@data-timestamp]");
        if (bspNode == null)
            return null;

        //var timestampStr = bspNode.GetAttributeValue("data-timestamp", null);
        var timestampStr = bspNode.GetAttributeValue("data-timestamp", string.Empty);
        if (long.TryParse(timestampStr, out long unixTimestamp))
            return unixTimestamp;

        logger.Log($"Failed to parse Unix timestamp from: {timestampStr}", LogLevel.Warning);
        return null;
    }

    private static string TrimInnerHtmlWhitespace(string html)
    {
        // Replace multiple whitespace (including newlines/tabs) with a single space
        return Regex.Replace(html, @"\s+", " ").Trim();
    }
}
