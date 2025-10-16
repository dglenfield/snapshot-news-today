using Common.Logging;
using HtmlAgilityPack;
using NewsScraper.Models;
using System.Buffers.Text;
using System.Net.ServerSentEvents;
using System.Xml.Linq;

namespace NewsScraper.Providers;

internal class ApNewsArticleProvider(Logger logger)
{
    string articleUrl = @"https://apnews.com/article/alaska-halong-flooding-east-coast-noreaster-44668913640e8482202320d38f08788e";

    public async Task<SourceArticle> GetArticle()
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

        SourceArticle article = new()
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

    private int GetMainStoryArticles(HtmlDocument htmlDoc)
    {
        string baseUrl = "https://apnews.com";

        int totalArticleCount = 0;
        int mainArticleCount = 0;
        var storySections = htmlDoc.DocumentNode.SelectNodes("//div[normalize-space(@class) = 'PageListStandardE']");
        foreach (var storySection in storySections)
        {
            // Find the main article in the story section
            var mainArticle = storySection.SelectSingleNode(".//div[normalize-space(@class) = 'PageListStandardE-leadPromo-info']");
            var mainArticleUrl = mainArticle.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
            var mainHeadline = mainArticle.SelectSingleNode(".//span").InnerText;
            if (!mainArticleUrl.StartsWith($"{baseUrl}/article") && !mainArticleUrl.StartsWith($"{baseUrl}/live"))
                continue;
            logger.Log($"Main Article {++mainArticleCount}");
            logger.Log($"  {mainHeadline}", logAsRawMessage: true);
            logger.Log($"  {mainArticleUrl}", logAsRawMessage: true);
            totalArticleCount++;
            
            var secondaryArticles = storySection.SelectSingleNode(".//div[normalize-space(@class) = 'PageListStandardE-items-secondary']");
            if (secondaryArticles is null)
            {
                logger.Log("----------------------------------------------", logAsRawMessage: true);
                continue;
            }
            int secondaryArticleCount = 0;
            foreach (var secondaryArticle in secondaryArticles.SelectNodes(".//div[normalize-space(@class) = 'PagePromo']"))
            {
                var articleUrl = secondaryArticle.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
                var headline = secondaryArticle.SelectSingleNode(".//span").InnerText;
                var articleUnixTimestamp = secondaryArticle.SelectSingleNode(".//bsp-timestamp[@data-timestamp]")?.GetAttributeValue("data-timestamp", "");
                logger.Log($"Secondary Article {++secondaryArticleCount}", logAsRawMessage: true);
                logger.Log($"  {headline}", logAsRawMessage: true);
                logger.Log($"  {articleUrl}", logAsRawMessage: true);
                if (long.TryParse(articleUnixTimestamp, out long timestamp))
                {
                    DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
                    DateTime dateTime = dateTimeOffset.LocalDateTime;
                    logger.Log($"  Last Updated (Local): {dateTime}", logAsRawMessage: true);
                }
                else
                {
                    logger.Log($"  ArticleUnixTimestamp = {articleUnixTimestamp}", logAsRawMessage: true);
                }
                totalArticleCount++;
            }
            logger.Log("----------------------------------------------", logAsRawMessage: true);
        }
        return totalArticleCount;
    }

    private int GetCBlockArticles(HtmlDocument htmlDoc)
    {
        var cBlockGrouping = htmlDoc.DocumentNode.SelectSingleNode("//div[normalize-space(@class) = 'PageListRightRailA' and @data-tb-region='C block']");
        var firstOtherArticle = cBlockGrouping.SelectSingleNode("//div[normalize-space(@class) = 'PageList-items-first']");
        var articleUrl = firstOtherArticle.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
        var headline = firstOtherArticle.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText;
        var articleUnixTimestamp = firstOtherArticle.SelectSingleNode(".//bsp-timestamp[@data-timestamp]")?.GetAttributeValue("data-timestamp", "");
        logger.Log($"First C Block Article", logAsRawMessage: true);
        logger.Log($"  {headline}", logAsRawMessage: true);
        logger.Log($"  {articleUrl}", logAsRawMessage: true);
        if (long.TryParse(articleUnixTimestamp, out long timestamp))
        {
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
            DateTime dateTime = dateTimeOffset.LocalDateTime;
            logger.Log($"  Last Updated (Local): {dateTime}", logAsRawMessage: true);
        }
        else
        {
            logger.Log($"  ArticleUnixTimestamp = {articleUnixTimestamp}", logAsRawMessage: true);
        }

        var otherArticles = cBlockGrouping.SelectNodes(".//li[normalize-space(@class) = 'PageList-items-item']");
        int count = 1;
        foreach (var otherArticle in otherArticles)
        {
            var otherArticleUrl = otherArticle.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
            var otherHeadline = otherArticle.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText;
            var otherArticleUnixTimestamp = otherArticle.SelectSingleNode(".//div[normalize-space(@class) = 'PagePromo']")?.GetAttributeValue("data-updated-date-timestamp", "");
            logger.Log($"C Block Article {++count}", logAsRawMessage: true);
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
        return count;
    }

    private int GetListBArticles(HtmlDocument htmlDoc)
    {
        var listBGrouping = htmlDoc.DocumentNode.SelectSingleNode("//bsp-list-loadmore[normalize-space(@class) = 'PageListStandardB' and @data-gtm-modulestyle='List B']");
        //logger.Log($"{TrimInnerHtmlWhitespace(listBGrouping.OuterHtml)}", logAsRawMessage: true);
        var listBArticles = listBGrouping.SelectNodes(".//div[normalize-space(@class) = 'PageList-items-item']");
        int listBCount = 0;
        foreach (var listBArticle in listBArticles)
        {
            var otherArticleUrl = listBArticle.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
            var otherHeadline = listBArticle.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText;
            //var otherArticleUnixTimestamp = listBArticle.SelectSingleNode(".//bsp-timestamp[@data-timestamp]")?.GetAttributeValue("data-timestamp", "");
            var otherArticleUnixTimestamp = listBArticle.SelectSingleNode(".//div[normalize-space(@class) = 'PagePromo']")?.GetAttributeValue("data-updated-date-timestamp", "");
            logger.Log($"List B Article {++listBCount}", logAsRawMessage: true);
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
        return listBCount;
    }

    private int GetMostReadArticles(HtmlDocument htmlDoc)
    {
        var mostReadGrouping = htmlDoc.DocumentNode.SelectSingleNode("//div[normalize-space(@class) = 'PageListRightRailA' and @data-tb-region='Most read']");
        //logger.Log($"{TrimInnerHtmlWhitespace(mostReadGrouping.OuterHtml)}", logAsRawMessage: true);
        var mostReadArticles = mostReadGrouping.SelectNodes(".//li[normalize-space(@class) = 'PageList-items-item']");
        int mostReadCount = 0;
        foreach (var mostReadArticle in mostReadArticles)
        {
            var otherArticleUrl = mostReadArticle.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
            var otherHeadline = mostReadArticle.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText;
            //var otherArticleUnixTimestamp = listBArticle.SelectSingleNode(".//bsp-timestamp[@data-timestamp]")?.GetAttributeValue("data-timestamp", "");
            var otherArticleUnixTimestamp = mostReadArticle.SelectSingleNode(".//div[normalize-space(@class) = 'PagePromo']")?.GetAttributeValue("data-updated-date-timestamp", "");
            logger.Log($"Most Read Article {++mostReadCount}", logAsRawMessage: true);
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
        return mostReadCount;
    }

    private int GetBBlockLatestPublishedArticles(HtmlDocument htmlDoc)
    {
        var bBlockLatestPublishedGrouping = htmlDoc.DocumentNode.SelectSingleNode("//div[normalize-space(@class) = 'PageListRightRailA' and @data-tb-region='B2']");
        var bBlockLatestPublishedArticles = bBlockLatestPublishedGrouping.SelectNodes(".//div[normalize-space(@class) = 'PagePromo']");
        int bBlockLatestPublishedCount = 0;
        foreach (var bBlockLatestPublishedArticle in bBlockLatestPublishedArticles)
        {
            var otherArticleUrl = bBlockLatestPublishedArticle.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
            var otherHeadline = bBlockLatestPublishedArticle.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText;
            //var otherArticleUnixTimestamp = listBArticle.SelectSingleNode(".//bsp-timestamp[@data-timestamp]")?.GetAttributeValue("data-timestamp", "");
            var otherArticleUnixTimestamp = bBlockLatestPublishedArticle.GetAttributeValue("data-updated-date-timestamp", "");
            logger.Log($"B Block (Latest Published) Article {++bBlockLatestPublishedCount}", logAsRawMessage: true);
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
        return bBlockLatestPublishedCount;
    }

    private int GetIcymiArticles(HtmlDocument htmlDoc)
    {
        var icymiGrouping = htmlDoc.DocumentNode.SelectSingleNode("//div[@data-tb-region='ICYMI']");
        //logger.Log($"{TrimInnerHtmlWhitespace(icymiGrouping.OuterHtml)}", logAsRawMessage: true);
        var icymiArticles = icymiGrouping.SelectNodes(".//div[normalize-space(@class) = 'PagePromo']");
        int icymiCount = 0;
        foreach (var icymiArticle in icymiArticles)
        {
            var otherArticleUrl = icymiArticle.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
            var otherHeadline = icymiArticle.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText;
            var otherArticleUnixTimestamp = icymiArticle.GetAttributeValue("data-updated-date-timestamp", "");
            logger.Log($"ICYMI Article {++icymiCount}", logAsRawMessage: true);
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
        return icymiCount;
    }

    private int GetBeWellArticles(HtmlDocument htmlDoc)
    {
        var beWellGrouping = htmlDoc.DocumentNode.SelectSingleNode("//div[@data-gtm-region='be well headline queue']");
        var beWellArticles = beWellGrouping.SelectNodes(".//div[normalize-space(@class) = 'PagePromo']");
        int beWellCount = 0;
        foreach (var beWellArticle in beWellArticles)
        {
            var otherArticleUrl = beWellArticle.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
            var otherHeadline = beWellArticle.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText;
            var otherArticleUnixTimestamp = beWellArticle.GetAttributeValue("data-updated-date-timestamp", "");
            logger.Log($"Be Well Article {++beWellCount}", logAsRawMessage: true);
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
        return beWellCount;
    }

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

    private void GetAllLinks(HtmlDocument htmlDoc)
    {
        string baseUrl = "https://apnews.com";
        string articlesPrefix = $"{baseUrl}/article/";
        string livePrefix = $"{baseUrl}/live/";

        var links = htmlDoc.DocumentNode
            .SelectNodes("//a[@href]")
            ?.Select(node => node.GetAttributeValue("href", ""))
            .Where(href =>
                href.StartsWith(articlesPrefix, StringComparison.OrdinalIgnoreCase) ||
                href.StartsWith(livePrefix, StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToList() ?? new List<string>();

        logger.Log($"Total links found on {baseUrl}: {links.Count}", LogLevel.Info);
        foreach (var link in links)
        {
            //logger.Log($"Found link: {link}", LogLevel.Info);
        }
    }

    public async Task<List<SourceArticle>?> GetArticles()
    {
        int articleCount = 0;

        HtmlDocument htmlDoc = new();
        //htmlDoc.LoadHtml(await new HttpClient().GetStringAsync(baseUrl));
        htmlDoc.Load(@"C:/Users/danny/OneDrive/Projects/SnapshotNewsToday/TestData/AssociatedPressNews.html");

        // Get Main Story Articles
        articleCount += GetMainStoryArticles(htmlDoc);
        // Get news articles on right side (C block)
        articleCount += GetCBlockArticles(htmlDoc);
        // Get Horizontal News Articles (List B)
        articleCount += GetListBArticles(htmlDoc);
        // Get Most Read (These can be duplicates of other articles but it's good to mark as "Most Read"
        articleCount += GetMostReadArticles(htmlDoc);
        // B Block (Latest Published) Articles
        articleCount += GetBBlockLatestPublishedArticles(htmlDoc);
        // In Case You Missed It (ICYMI) Articles
        articleCount += GetIcymiArticles(htmlDoc);
        // Be Well Articles
        articleCount += GetBeWellArticles(htmlDoc);
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

        GetAllLinks(htmlDoc); // TODO: Compare articles found with all links

        return null;


        // div class="PagePromo-content"
        //var promoDivs = htmlDoc.DocumentNode
        //    .SelectNodes("//div[contains(@class, 'PagePromo-content')]")
        //    ?.ToList() ?? new List<HtmlNode>();

        //logger.Log($"Total promo divs found on {baseUrl}: {promoDivs.Count}", LogLevel.Info);
        //foreach (var div in promoDivs)
        //{
        //    string html = TrimInnerHtmlWhitespace(div.InnerHtml.Trim());
        //    string url = div.SelectSingleNode(".//a[@href]")?.GetAttributeValue("href", "").Trim() ?? "N/A";
        //    string headline = div.SelectSingleNode(".//h1")?.InnerText.Trim() ?? "N/A";
        //    string timestamp = "";
        //    logger.Log($"Found promo div: {html}", LogLevel.Info);
        //}


        //var promoDivs = htmlDoc.DocumentNode
        //    .SelectNodes("//div[contains(@class, 'PageListStandardE-leadPromo-info')]")
        //    ?.ToList() ?? new List<HtmlNode>();

        //logger.Log($"Total promo divs found on {baseUrl}: {promoDivs.Count}", LogLevel.Info);
        //foreach (var div in promoDivs)
        //{
        //    string html = TrimInnerHtmlWhitespace(div.OuterHtml.Trim());
        //    string url = div.SelectSingleNode(".//a[@href]")?.GetAttributeValue("href", "").Trim() ?? "N/A";
        //    string headline = div.SelectSingleNode(".//h1")?.InnerText.Trim() ?? "N/A";
        //    string timestamp = "";
        //    logger.Log($"Found promo div: {html}", LogLevel.Info);
        //}



        //return null;

        //string html = TrimInnerHtmlWhitespace(articlesDiv.InnerHtml.Trim());
        ////logger.Log($"Articles Div Inner HTML: {html}", LogLevel.Info);
        //if (articlesDiv != null)
        //{
        //    foreach (var node in articlesDiv.SelectNodes(".//*") ?? Enumerable.Empty<HtmlNode>())
        //    {
        //        if (node.Name == "img")
        //            continue; // Skip image tags

        //        string classes = node.GetAttributeValue("class", "");
        //        //logger.Log($"Tag: {node.Name}, Classes: {classes}", LogLevel.Info);

        //        if (classes == "PageListStandardE-leadPromo-media" ||
        //            classes == "PageListStandardE-leadPromo-media" ||
        //            classes == "PagePromo-media")
        //            continue; // Skip elements without class attributes)

        //        if (classes == "PageListStandardE-leadPromo-info") 
        //        {
        //            logger.Log($"{TrimInnerHtmlWhitespace(node.InnerHtml.Trim())}", LogLevel.Info);
        //        }


        //        // Process every descendant element
        //        //logger.Log($"Tag: {node.Name}, Classes: {classes}", LogLevel.Info);
        //        //logger.Log($"Tag: {node.Name}, Text: {node.InnerText.Trim()}", LogLevel.Info);
        //        //if (node.Name == "a")
        //        //{
        //        //    string href = node.GetAttributeValue("href", "");
        //        //    logger.Log($"Found hyperlink: {href}", LogLevel.Info);
        //        //}
        //    }


        //}

    }

    public async Task<List<string>> GetHyperlinksStartingWithBaseUrl()
    {
        string baseUrl = "https://apnews.com";

        HtmlDocument htmlDoc = new();
        htmlDoc.LoadHtml(await new HttpClient().GetStringAsync(baseUrl));

        var links = htmlDoc.DocumentNode
            .SelectNodes("//a[@href]")
            ?.Select(node => node.GetAttributeValue("href", ""))
            .Where(href => href.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToList() ?? new List<string>();

        return links;
    }

    public async Task<List<string>> GetHyperlinksStartingWithBaseUrlArticlesOrLive()
    {
        string baseUrl = "https://apnews.com";
        string articlesPrefix = $"{baseUrl}/articles/";
        string livePrefix = $"{baseUrl}/live/";

        HtmlDocument htmlDoc = new();
        htmlDoc.LoadHtml(await new HttpClient().GetStringAsync(baseUrl));

        var links = htmlDoc.DocumentNode
            .SelectNodes("//a[@href]")
            ?.Select(node => node.GetAttributeValue("href", ""))
            .Where(href =>
                href.StartsWith(articlesPrefix, StringComparison.OrdinalIgnoreCase) ||
                href.StartsWith(livePrefix, StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToList() ?? new List<string>();

        return links;
    }

    public string? ExtractModifiedDate(HtmlDocument htmlDoc)
    {
        var dateDiv = htmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'Page-dateModified')]");
        if (dateDiv == null)
            return null;

        logger.Log($"Date Div Inner HTML: {dateDiv.InnerHtml}", LogLevel.Debug);
        var spanNode = dateDiv.SelectSingleNode(".//span[@data-date]");
        return spanNode?.InnerText.Trim();
    }

    public long? ExtractUnixTimestamp(HtmlDocument htmlDoc)
    {
        var bspNode = htmlDoc.DocumentNode.SelectSingleNode("//bsp-timestamp[@data-timestamp]");
        if (bspNode == null)
            return null;

        var timestampStr = bspNode.GetAttributeValue("data-timestamp", null);
        if (long.TryParse(timestampStr, out long unixTimestamp))
            return unixTimestamp;

        logger.Log($"Failed to parse Unix timestamp from: {timestampStr}", LogLevel.Warning);
        return null;
    }

    private static string TrimInnerHtmlWhitespace(string html)
    {
        // Replace multiple whitespace (including newlines/tabs) with a single space
        return System.Text.RegularExpressions.Regex.Replace(html, @"\s+", " ").Trim();
    }
}
