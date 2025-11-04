using Common.Data.Repositories;
using Common.Logging;
using Common.Models;
using Common.Models.AssociatedPress;
using Common.Models.AssociatedPress.ArticlePage;
using Common.Models.AssociatedPress.MainPage;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace NewsSnapshot.Scrapers.AssociatedPress.ArticlePage;

internal class ArticlePageScraper(APNewsArticleRepository articleRepository, Logger logger)
{
    public async Task<APNewsArticle> ScrapeAsync(APNewsHeadline headline, APNewsScrape job)
    {
        APNewsArticle article = new()
        {
            HeadlineId = headline.Id,
            ScrapedOn = DateTime.UtcNow,
            SourceUri = headline.TargetUri,
            TestFile = job.ArticlePageTestFile
        };
        
        try
        {
            // Create the stub article record in the database
            article.Id = await articleRepository.CreateAsync(article);

            // Get the Main Page HTML or the HTML test file
            HtmlDocument htmlDocument = new();
            if (job.UseArticlePageTestFile && !string.IsNullOrWhiteSpace(job.ArticlePageTestFile))
                htmlDocument.Load(job.ArticlePageTestFile);
            else
                htmlDocument.LoadHtml(await new HttpClient().GetStringAsync(article.SourceUri));

            // Headline is optional, continue processing if not found
            HtmlNode? headlineNode = htmlDocument.DocumentNode.SelectSingleNode("//h1[normalize-space(@class) = 'Page-headline']");
            string articleHeadline = TrimInnerHtmlWhitespace(headlineNode?.InnerText.Trim() ?? string.Empty);
            article.Headline = !string.IsNullOrWhiteSpace(articleHeadline) ? articleHeadline : null;

            // Author is optional, continue processing if not found
            HtmlNode? authorNode = htmlDocument.DocumentNode.SelectSingleNode("//div[normalize-space(@class) = 'Page-authors']");
            string author = TrimInnerHtmlWhitespace(authorNode?.InnerText.Replace("By", "").Replace("&nbsp;", "").Trim() ?? string.Empty);
            article.Author = string.IsNullOrWhiteSpace(author) ? null : author;

            // Last Updated On is optional, continue processing if not found
            HtmlNode? modifiedDateNode = htmlDocument.DocumentNode.SelectSingleNode("//div[normalize-space(@class) = 'Page-dateModified']");
            string? unixTimestamp = modifiedDateNode?.SelectSingleNode(".//bsp-timestamp[@data-timestamp]")?.GetAttributeValue("data-timestamp", "");
            DateTime? lastUpdatedOn = string.IsNullOrWhiteSpace(unixTimestamp) ? null : ConvertUnixTimestamp(unixTimestamp);
            article.LastUpdatedOn = lastUpdatedOn;

            // Article Content is required, throw an exception if not found
            HtmlNode contentNode = htmlDocument.DocumentNode.SelectSingleNode("//div[contains(@class, 'RichTextStoryBody')]") 
                ?? throw new NodeNotFoundException("Article content node not found.");
            List<string> paragraphs = [];
            foreach (var paragraphNode in contentNode.SelectNodes(".//p"))
            {
                string paragraph = TrimInnerHtmlWhitespace(paragraphNode.InnerText.Trim());
                if (!string.IsNullOrWhiteSpace(paragraph))
                    paragraphs.Add(paragraph);
            }
            article.ContentParagraphs = paragraphs.Count > 0 ? paragraphs : throw new Exception("Article content not found.");
        }
        catch (NodeNotFoundException ex)
        {
            article.ScrapeException = new JobException() { Source = $"XPath error in {nameof(ArticlePageScraper)}.{nameof(ScrapeAsync)}", Exception = ex };
        }
        catch (Exception ex)
        {
            article.ScrapeException = new JobException() { Source = $"{nameof(ArticlePageScraper)}.{nameof(ScrapeAsync)}", Exception = ex };
        }

        try
        {
            // Update the article in the database
            if (article.ScrapeException is null) 
                article.IsSuccess = true;
            else
                article.IsSuccess = false;
            await articleRepository.UpdateAsync(article);
        }
        catch (Exception ex)
        {
            article.IsSuccess = false;
            logger.Log("Updating the article in the database failed!", LogLevel.Error);
            logger.LogException(ex);
        }

        return article;
    }

    private DateTime? ConvertUnixTimestamp(string unixTimestamp)
    {
        if (long.TryParse(unixTimestamp, out long timestamp))
        {
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
            return dateTimeOffset.UtcDateTime;
        }
        return null;
    }

    private string TrimInnerHtmlWhitespace(string html)
    {
        // Replace multiple whitespace (including newlines/tabs) with a single space
        return Regex.Replace(html, @"\s+", " ").Trim();
    }
}
