using Common.Logging;
using HtmlAgilityPack;
using NewsScraper.Models;
using NewsScraper.Models.AssociatedPress.MainPage;
using NewsScraper.Models.CNN;
using System.Text.RegularExpressions;

namespace NewsScraper.Providers;

internal class AssociatedPressArticleProvider(Logger logger)
{
    private int GetUSNewsArticles(HtmlDocument htmlDoc)
    {
        var usNewsGrouping = htmlDoc.DocumentNode.SelectSingleNode("//div[@data-gtm-topic='Topics - US News']");
        var usNewsArticles = usNewsGrouping.SelectNodes(".//div[normalize-space(@class) = 'PagePromo']");
        int usNewsCount = 0;
        foreach (var usNewsArticle in usNewsArticles)
        {
            var otherArticleUrl = usNewsArticle.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
            var otherHeadline = usNewsArticle.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText;
            var otherArticleUnixTimestamp = usNewsArticle.GetAttributeValue("data-updated-date-timestamp", "");
            logger.Log($"US News Article {++usNewsCount}", logAsRawMessage: true);
            logger.Log($"  {otherHeadline}", logAsRawMessage: true);
            logger.Log($"  {otherArticleUrl}", logAsRawMessage: true);
            if (long.TryParse(otherArticleUnixTimestamp, out long otherArticleTimestamp))
            {
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(otherArticleTimestamp);
                DateTime dateTime = dateTimeOffset.LocalDateTime;
                logger.Log($"  Last Updated (Local): {dateTime}", logAsRawMessage: true);
            }
            else
            {
                logger.Log($"  ArticleUnixTimestamp = {otherArticleUnixTimestamp}", logAsRawMessage: true);
            }
        }
        logger.Log("----------------------------------------------", logAsRawMessage: true);
        return usNewsCount;
    }

    private int GetWorldNewsArticles(HtmlDocument htmlDoc)
    {
        var worldNewsGrouping = htmlDoc.DocumentNode.SelectSingleNode(
            "//div[normalize-space(@class) = 'PageListRightRailA' " +
            "and @data-tb-region='Topics - Sports' " +
            "and .//h2/a[contains(normalize-space(text()), 'WORLD NEWS')]]");

        if (worldNewsGrouping == null)
        {
            logger.Log("World News section not found with strict selector, trying fallback...",
                LogLevel.Warning, logAsRawMessage: true);

            // Fallback: just look for the heading
            worldNewsGrouping = htmlDoc.DocumentNode.SelectSingleNode(
                "//div[.//h2/a[contains(normalize-space(text()), 'WORLD NEWS')]]");
        }
        var worldNewsArticles = worldNewsGrouping.SelectNodes(".//div[normalize-space(@class) = 'PagePromo']");
        int worldNewsCount = 0;
        foreach (var worldNewsArticle in worldNewsArticles)
        {
            var otherArticleUrl = worldNewsArticle.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
            var otherHeadline = worldNewsArticle.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText;
            var otherArticleUnixTimestamp = worldNewsArticle.GetAttributeValue("data-updated-date-timestamp", "");
            logger.Log($"World News Article {++worldNewsCount}", logAsRawMessage: true);
            logger.Log($"  {otherHeadline}", logAsRawMessage: true);
            logger.Log($"  {otherArticleUrl}", logAsRawMessage: true);
            if (long.TryParse(otherArticleUnixTimestamp, out long otherArticleTimestamp))
            {
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(otherArticleTimestamp);
                DateTime dateTime = dateTimeOffset.LocalDateTime;
                logger.Log($"  Last Updated (Local): {dateTime}", logAsRawMessage: true);
            }
            else
            {
                logger.Log($"  ArticleUnixTimestamp = {otherArticleUnixTimestamp}", logAsRawMessage: true);
            }
        }
        logger.Log("----------------------------------------------", logAsRawMessage: true);
        return worldNewsCount;
    }

    private int GetPoliticsArticles(HtmlDocument htmlDoc)
    {
        var articleGrouping = htmlDoc.DocumentNode.SelectSingleNode("//div[@data-gtm-topic='Topics - Politics']");
        var articles = articleGrouping.SelectNodes(".//div[normalize-space(@class) = 'PagePromo']");
        int articleCount = 0;
        foreach (var article in articles)
        {
            var articleUrl = article.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
            var headline = article.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText;
            var unixTimestamp = article.GetAttributeValue("data-updated-date-timestamp", "");
            logger.Log($"Politics Article {++articleCount}", logAsRawMessage: true);
            logger.Log($"  {headline}", logAsRawMessage: true);
            logger.Log($"  {articleUrl}", logAsRawMessage: true);
            if (long.TryParse(unixTimestamp, out long timestamp))
            {
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
                DateTime dateTime = dateTimeOffset.LocalDateTime;
                logger.Log($"  Last Updated (Local): {dateTime}", logAsRawMessage: true);
            }
            else
            {
                logger.Log($"  ArticleUnixTimestamp = {unixTimestamp}", logAsRawMessage: true);
            }
        }
        logger.Log("----------------------------------------------", logAsRawMessage: true);
        return articleCount;
    }

    private int GetEntertainmentArticles(HtmlDocument htmlDoc)
    {
        var articleGrouping = htmlDoc.DocumentNode.SelectSingleNode("//div[@data-gtm-topic='Topics - Entertainment']");
        var articles = articleGrouping.SelectNodes(".//div[normalize-space(@class) = 'PagePromo']");
        int articleCount = 0;
        foreach (var article in articles)
        {
            var articleUrl = article.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
            var headline = article.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText;
            var unixTimestamp = article.GetAttributeValue("data-updated-date-timestamp", "");
            logger.Log($"Entertainment Article {++articleCount}", logAsRawMessage: true);
            logger.Log($"  {headline}", logAsRawMessage: true);
            logger.Log($"  {articleUrl}", logAsRawMessage: true);
            if (long.TryParse(unixTimestamp, out long timestamp))
            {
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
                DateTime dateTime = dateTimeOffset.LocalDateTime;
                logger.Log($"  Last Updated (Local): {dateTime}", logAsRawMessage: true);
            }
            else
            {
                logger.Log($"  ArticleUnixTimestamp = {unixTimestamp}", logAsRawMessage: true);
            }
        }
        logger.Log("----------------------------------------------", logAsRawMessage: true);
        return articleCount;
    }

    private int GetSportsArticles(HtmlDocument htmlDoc)
    {
        var sportsGrouping = htmlDoc.DocumentNode.SelectSingleNode(
            "//div[normalize-space(@class) = 'PageListRightRailA' " +
            "and @data-tb-region='Topics - World News' " +
            "and .//h2/a[contains(normalize-space(text()), 'SPORTS')]]");

        if (sportsGrouping == null)
        {
            logger.Log("Sports section not found with strict selector, trying fallback...",
                LogLevel.Warning, logAsRawMessage: true);

            // Fallback: just look for the heading
            sportsGrouping = htmlDoc.DocumentNode.SelectSingleNode(
                "//div[.//h2/a[contains(normalize-space(text()), 'SPORTS')]]");
        }
        var sportsArticles = sportsGrouping.SelectNodes(".//div[normalize-space(@class) = 'PagePromo']");
        int count = 0;
        foreach (var srticle in sportsArticles)
        {
            var articleUrl = srticle.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
            var headline = srticle.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText;
            var unixTimestamp = srticle.GetAttributeValue("data-updated-date-timestamp", "");
            logger.Log($"Sports Article {++count}", logAsRawMessage: true);
            logger.Log($"  {headline}", logAsRawMessage: true);
            logger.Log($"  {articleUrl}", logAsRawMessage: true);
            if (long.TryParse(unixTimestamp, out long timestamp))
            {
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
                DateTime dateTime = dateTimeOffset.LocalDateTime;
                logger.Log($"  Last Updated (Local): {dateTime}", logAsRawMessage: true);
            }
            else
            {
                logger.Log($"  ArticleUnixTimestamp = {unixTimestamp}", logAsRawMessage: true);
            }
        }
        logger.Log("----------------------------------------------", logAsRawMessage: true);
        return count;
    }

    private int GetBusinessArticles(HtmlDocument htmlDoc)
    {
        var articleGrouping = htmlDoc.DocumentNode.SelectSingleNode("//div[@data-gtm-topic='Topics - Business']");
        var articles = articleGrouping.SelectNodes(".//div[normalize-space(@class) = 'PagePromo']");
        int articleCount = 0;
        foreach (var article in articles)
        {
            var articleUrl = article.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
            var headline = article.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText;
            var unixTimestamp = article.GetAttributeValue("data-updated-date-timestamp", "");
            logger.Log($"Business Article {++articleCount}", logAsRawMessage: true);
            logger.Log($"  {headline}", logAsRawMessage: true);
            logger.Log($"  {articleUrl}", logAsRawMessage: true);
            if (long.TryParse(unixTimestamp, out long timestamp))
            {
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
                DateTime dateTime = dateTimeOffset.LocalDateTime;
                logger.Log($"  Last Updated (Local): {dateTime}", logAsRawMessage: true);
            }
            else
            {
                logger.Log($"  ArticleUnixTimestamp = {unixTimestamp}", logAsRawMessage: true);
            }
        }
        logger.Log("----------------------------------------------", logAsRawMessage: true);
        return articleCount;
    }

    private int GetScienceArticles(HtmlDocument htmlDoc)
    {
        var articleGrouping = htmlDoc.DocumentNode.SelectSingleNode("//div[@data-gtm-region='Topics - Science']");
        var articles = articleGrouping.SelectNodes(".//div[normalize-space(@class) = 'PagePromo']");
        int articleCount = 0;
        foreach (var article in articles)
        {
            var articleUrl = article.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
            var headline = article.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText;
            var unixTimestamp = article.GetAttributeValue("data-updated-date-timestamp", "");
            logger.Log($"Science Article {++articleCount}", logAsRawMessage: true);
            logger.Log($"  {headline}", logAsRawMessage: true);
            logger.Log($"  {articleUrl}", logAsRawMessage: true);
            if (long.TryParse(unixTimestamp, out long timestamp))
            {
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
                DateTime dateTime = dateTimeOffset.LocalDateTime;
                logger.Log($"  Last Updated (Local): {dateTime}", logAsRawMessage: true);
            }
            else
            {
                logger.Log($"  ArticleUnixTimestamp = {unixTimestamp}", logAsRawMessage: true);
            }
        }
        logger.Log("----------------------------------------------", logAsRawMessage: true);
        return articleCount;
    }

    private int GetLifestyleArticles(HtmlDocument htmlDoc)
    {
        var articleGrouping = htmlDoc.DocumentNode.SelectSingleNode("//div[@data-gtm-region='Topics - Lifestyle']");
        var articles = articleGrouping.SelectNodes(".//div[normalize-space(@class) = 'PagePromo']");
        int articleCount = 0;
        foreach (var article in articles)
        {
            var articleUrl = article.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
            var headline = article.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText;
            var unixTimestamp = article.GetAttributeValue("data-updated-date-timestamp", "");
            logger.Log($"Lifestyle Article {++articleCount}", logAsRawMessage: true);
            logger.Log($"  {headline}", logAsRawMessage: true);
            logger.Log($"  {articleUrl}", logAsRawMessage: true);
            if (long.TryParse(unixTimestamp, out long timestamp))
            {
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
                DateTime dateTime = dateTimeOffset.LocalDateTime;
                logger.Log($"  Last Updated (Local): {dateTime}", logAsRawMessage: true);
            }
            else
            {
                logger.Log($"  ArticleUnixTimestamp = {unixTimestamp}", logAsRawMessage: true);
            }
        }
        logger.Log("----------------------------------------------", logAsRawMessage: true);
        return articleCount;
    }

    private int GetTechnologyArticles(HtmlDocument htmlDoc)
    {
        var articleGrouping = htmlDoc.DocumentNode.SelectSingleNode(
            "//div[normalize-space(@class) = 'PageListRightRailA' " +
            "and @data-tb-region='Topics - Election 2024' " +
            "and .//h2/a[contains(normalize-space(text()), 'Technology')]]");

        if (articleGrouping == null)
        {
            logger.Log("Technology section not found with strict selector, trying fallback...",
                LogLevel.Warning, logAsRawMessage: true);

            // Fallback: just look for the heading
            articleGrouping = htmlDoc.DocumentNode.SelectSingleNode(
                "//div[.//h2/a[contains(normalize-space(text()), 'Technology')]]");
        }

        var articles = articleGrouping.SelectNodes(".//div[normalize-space(@class) = 'PagePromo']");
        int articleCount = 0;
        foreach (var article in articles)
        {
            var articleUrl = article.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
            var headline = article.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText;
            var unixTimestamp = article.GetAttributeValue("data-updated-date-timestamp", "");
            logger.Log($"Technology Article {++articleCount}", logAsRawMessage: true);
            logger.Log($"  {headline}", logAsRawMessage: true);
            logger.Log($"  {articleUrl}", logAsRawMessage: true);
            if (long.TryParse(unixTimestamp, out long timestamp))
            {
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
                DateTime dateTime = dateTimeOffset.LocalDateTime;
                logger.Log($"  Last Updated (Local): {dateTime}", logAsRawMessage: true);
            }
            else
            {
                logger.Log($"  ArticleUnixTimestamp = {unixTimestamp}", logAsRawMessage: true);
            }
        }
        logger.Log("----------------------------------------------", logAsRawMessage: true);
        return articleCount;
    }

    private int GetHealthArticles(HtmlDocument htmlDoc)
    {
        var articleGrouping = htmlDoc.DocumentNode.SelectSingleNode("//div[@data-gtm-region='Topics - Be Well']");
        var articles = articleGrouping.SelectNodes(".//div[normalize-space(@class) = 'PagePromo']");
        int articleCount = 0;
        foreach (var article in articles)
        {
            var articleUrl = article.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
            var headline = article.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText;
            var unixTimestamp = article.GetAttributeValue("data-updated-date-timestamp", "");
            logger.Log($"Health Article {++articleCount}", logAsRawMessage: true);
            logger.Log($"  {headline}", logAsRawMessage: true);
            logger.Log($"  {articleUrl}", logAsRawMessage: true);
            if (long.TryParse(unixTimestamp, out long timestamp))
            {
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
                DateTime dateTime = dateTimeOffset.LocalDateTime;
                logger.Log($"  Last Updated (Local): {dateTime}", logAsRawMessage: true);
            }
            else
            {
                logger.Log($"  ArticleUnixTimestamp = {unixTimestamp}", logAsRawMessage: true);
            }
        }
        logger.Log("----------------------------------------------", logAsRawMessage: true);
        return articleCount;
    }

    private int GetClimateArticles(HtmlDocument htmlDoc)
    {
        var articleGrouping = htmlDoc.DocumentNode.SelectSingleNode("//div[@data-gtm-region='Topics - Climate']");
        var articles = articleGrouping.SelectNodes(".//div[normalize-space(@class) = 'PagePromo']");
        int articleCount = 0;
        foreach (var article in articles)
        {
            var articleUrl = article.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
            var headline = article.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText;
            var unixTimestamp = article.GetAttributeValue("data-updated-date-timestamp", "");
            logger.Log($"Climate Article {++articleCount}", logAsRawMessage: true);
            logger.Log($"  {headline}", logAsRawMessage: true);
            logger.Log($"  {articleUrl}", logAsRawMessage: true);
            if (long.TryParse(unixTimestamp, out long timestamp))
            {
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
                DateTime dateTime = dateTimeOffset.LocalDateTime;
                logger.Log($"  Last Updated (Local): {dateTime}", logAsRawMessage: true);
            }
            else
            {
                logger.Log($"  ArticleUnixTimestamp = {unixTimestamp}", logAsRawMessage: true);
            }
        }
        logger.Log("----------------------------------------------", logAsRawMessage: true);
        return articleCount;
    }

    private int GetFactCheckArticles(HtmlDocument htmlDoc)
    {
        var articleGrouping = htmlDoc.DocumentNode.SelectSingleNode("//div[@data-gtm-region='Topics - Fact Check']");
        var articles = articleGrouping.SelectNodes(".//div[normalize-space(@class) = 'PagePromo']");
        int articleCount = 0;
        foreach (var article in articles)
        {
            var articleUrl = article.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
            var headline = article.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText;
            var unixTimestamp = article.GetAttributeValue("data-updated-date-timestamp", "");
            logger.Log($"Fact Check Article {++articleCount}", logAsRawMessage: true);
            logger.Log($"  {headline}", logAsRawMessage: true);
            logger.Log($"  {articleUrl}", logAsRawMessage: true);
            if (long.TryParse(unixTimestamp, out long timestamp))
            {
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
                DateTime dateTime = dateTimeOffset.LocalDateTime;
                logger.Log($"  Last Updated (Local): {dateTime}", logAsRawMessage: true);
            }
            else
            {
                logger.Log($"  ArticleUnixTimestamp = {unixTimestamp}", logAsRawMessage: true);
            }
        }
        logger.Log("----------------------------------------------", logAsRawMessage: true);
        return articleCount;
    }

    private int GetLatestNewsArticles(HtmlDocument htmlDoc)
    {
        var articleGrouping = htmlDoc.DocumentNode.SelectSingleNode("//bsp-list-loadmore[@data-gtm-region='Most Recent']");
        var articles = articleGrouping.SelectNodes(".//div[normalize-space(@class) = 'PagePromo']");
        int articleCount = 0;
        foreach (var article in articles)
        {
            var articleUrl = article.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
            var headline = article.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText;
            var unixTimestamp = article.GetAttributeValue("data-updated-date-timestamp", "");
            logger.Log($"Latest News Article {++articleCount}", logAsRawMessage: true);
            logger.Log($"  {headline}", logAsRawMessage: true);
            logger.Log($"  {articleUrl}", logAsRawMessage: true);
            if (long.TryParse(unixTimestamp, out long timestamp))
            {
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
                DateTime dateTime = dateTimeOffset.LocalDateTime;
                logger.Log($"  Last Updated (Local): {dateTime}", logAsRawMessage: true);
            }
            else
            {
                logger.Log($"  ArticleUnixTimestamp = {unixTimestamp}", logAsRawMessage: true);
            }
        }
        logger.Log("----------------------------------------------", logAsRawMessage: true);
        return articleCount;
    }

    public async Task<List<Article>?> GetArticles()
    {
        int articleCount = 0;
        List<PageSection> sourceGroups = [];

        HtmlDocument htmlDoc = new();
        //htmlDoc.LoadHtml(await new HttpClient().GetStringAsync(baseUrl));
        htmlDoc.Load(@"C:/Users/danny/OneDrive/Projects/SnapshotNewsToday/TestData/AssociatedPressNews.html");

        // US News Articles
        articleCount += GetUSNewsArticles(htmlDoc);
        // World News Articles (AP News mislabels this section as "Topics - Sports")
        articleCount += GetWorldNewsArticles(htmlDoc);
        // Politics Articles
        articleCount += GetPoliticsArticles(htmlDoc);
        // Entertainment Articles
        articleCount += GetEntertainmentArticles(htmlDoc);
        // Sports Articles (AP News has "Sports" as "Topics - World News")
        articleCount += GetSportsArticles(htmlDoc);
        // Business Articles
        articleCount += GetBusinessArticles(htmlDoc);
        // Science Articles
        articleCount += GetScienceArticles(htmlDoc);
        // Lifestyle Articles
        articleCount += GetLifestyleArticles(htmlDoc);
        // Technology Articles
        articleCount += GetTechnologyArticles(htmlDoc);
        // Health Articles
        articleCount += GetHealthArticles(htmlDoc);
        // Climate Articles
        articleCount += GetClimateArticles(htmlDoc);
        // Fact Check Articles
        articleCount += GetFactCheckArticles(htmlDoc);
        // Latest News Articles
        articleCount += GetLatestNewsArticles(htmlDoc);

        logger.Log($"Total articles = {articleCount}");

        return null;
    }

    private static string TrimInnerHtmlWhitespace(string html)
    {
        // Replace multiple whitespace (including newlines/tabs) with a single space
        return Regex.Replace(html, @"\s+", " ").Trim();
    }
}
