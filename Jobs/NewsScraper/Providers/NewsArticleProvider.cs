using Common.Logging;
using HtmlAgilityPack;
using NewsScraper.Models;

namespace NewsScraper.Providers;

internal class NewsArticleProvider(Logger logger)
{
    public async Task<SourceArticle> GetArticle(Uri articleUri)
    {
        logger.Log($"Fetching article content from {articleUri}", LogLevel.Info);

        if (articleUri.AbsoluteUri.Contains("videos/"))
        {
            logger.Log($"Video articles are not supported. Article URL: {articleUri}", LogLevel.Warning);
            throw new NotSupportedException("Video articles are not supported.");
        }

        HtmlDocument htmlDoc = new();
        if (Configuration.TestSettings.NewsArticleProvider.GetArticle.UseTestArticleFile)   
        {
            string testArticleFile = Configuration.TestSettings.NewsArticleProvider.GetArticle.TestArticleFile;
            if (string.IsNullOrEmpty(testArticleFile) || !File.Exists(testArticleFile))
            {
                logger.Log($"Test article file not found: {testArticleFile}", LogLevel.Error);
                throw new FileNotFoundException("Test article file not found.", testArticleFile);
            }
            logger.Log($"Loading article from test file: {testArticleFile}", LogLevel.Info);
            htmlDoc.Load(testArticleFile);
        }
        else
            htmlDoc.LoadHtml(await new HttpClient().GetStringAsync(articleUri.AbsoluteUri));

        // Extract publish date from data-first-publish attribute
        var timestampNode = htmlDoc.DocumentNode.SelectSingleNode("//span[contains(@class, 'timestamp__time-since')]");

        string? publishDateRaw = timestampNode?.GetAttributeValue("data-first-publish", string.Empty);
        if (DateTime.TryParse(publishDateRaw, out var parsedDate) == false)
            logger.Log($"Publish date not found or invalid. Raw Date: {publishDateRaw}", LogLevel.Warning);

        string? lastUpdatedDateRaw = timestampNode?.GetAttributeValue("data-last-publish", string.Empty);
        if (DateTime.TryParse(lastUpdatedDateRaw, out var parsedLastUpdatedDate) == false)
            logger.Log($"Last updated date not found or invalid. Raw Date: {lastUpdatedDateRaw}", LogLevel.Warning);

        // Extract author name  byline__name
        var authorNode = htmlDoc.DocumentNode.SelectSingleNode("//span[contains(@class, 'byline__name')]");

        // Extract headline
        var headlineNode = htmlDoc.DocumentNode.SelectSingleNode("//h1[contains(@class, 'headline__text')]");

        // Populate Article object
        SourceArticle article = new()
        {
            Headline = headlineNode?.InnerText.Trim(),
            Author = authorNode?.InnerText.Trim(),
            ArticleUri = articleUri,
            PublishDate = parsedDate.ToLocalTime(),
            LastUpdatedDate = parsedLastUpdatedDate.ToLocalTime()
        };

        // Extract article content paragraphs
        var articleContentNode = htmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'article__content')]");
        switch (articleContentNode is not null)
        {
            case true:
                foreach (var paragraph in articleContentNode.SelectNodes(".//p"))
                    article.ContentParagraphs.Add(paragraph.InnerText.Trim());
                break;
            case false:
                logger.Log("Article content node not found.", LogLevel.Warning);
                break;
        }

        return article;
    }
}
