using Common.Logging;
using HtmlAgilityPack;
using NewsScraper.Models.CNN;
using System.Text.RegularExpressions;

namespace NewsScraper.Processors;

internal class ArticlePageProcessor(Logger logger)
{
    string articleUrl = @"https://apnews.com/article/alaska-halong-flooding-east-coast-noreaster-44668913640e8482202320d38f08788e";

    public async Task<Article> GetArticle()
    {
        HtmlDocument htmlDoc = new();
        htmlDoc.LoadHtml(await new HttpClient().GetStringAsync(articleUrl));

        var headlineNode = htmlDoc.DocumentNode.SelectSingleNode("//h1");
        var authorNode = htmlDoc.DocumentNode.SelectSingleNode("//span[contains(@class, 'Component-headlineBylineAuthor')]");
        var publishDateNode = htmlDoc.DocumentNode.SelectSingleNode("//span[contains(@class, 'Component-headlineBylineDate')]");
        var contentNodes = htmlDoc.DocumentNode.SelectNodes("//p");
        string headline = headlineNode?.InnerText.Trim() ?? "N/A";
        string author = authorNode?.InnerText.Trim() ?? "N/A";
        string publishDate = publishDateNode?.InnerText.Trim() ?? "N/A";
        List<string> contentParagraphs = contentNodes?.Select(p => p.InnerText.Trim()).ToList() ?? new List<string>();
        logger.Log($"Headline: {headline}", LogLevel.Info);
        logger.Log($"Author: {author}", LogLevel.Info);
        logger.Log($"Publish Date: {publishDate}", LogLevel.Info);
        logger.Log("Content Paragraphs:", LogLevel.Info);
        foreach (var paragraph in contentParagraphs)
            logger.Log(paragraph, LogLevel.Info);

        var modifiedDate = ExtractUnixTimestamp(htmlDoc);
        if (modifiedDate.HasValue)
        {
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(modifiedDate.Value);
            DateTime localDateTime = dateTimeOffset.LocalDateTime;
            logger.Log($"Modified Date (Local): {localDateTime}", LogLevel.Info);
        }
        else
            logger.Log($"Modified Date: {modifiedDate}", LogLevel.Info);

        Article article = new()
        {
            ArticleUri = new Uri(articleUrl),
            JobRunId = 0,
            SourceName = "AP News",
            Headline = headline,
            Author = author,
            PublishDate = DateTime.TryParse(publishDate, out var pubDate) ? pubDate : null,
            ContentParagraphs = contentParagraphs,
            Success = true
        };

        return article;
    }

    private long? ExtractUnixTimestamp(HtmlDocument htmlDoc)
    {
        var bspNode = htmlDoc.DocumentNode.SelectSingleNode("//bsp-timestamp[@data-timestamp]");
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
