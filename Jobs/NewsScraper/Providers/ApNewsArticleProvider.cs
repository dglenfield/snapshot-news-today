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

    private void GetMainStoryArticles(HtmlDocument htmlDoc)
    {
        string baseUrl = "https://apnews.com";

        var storySections = htmlDoc.DocumentNode.SelectNodes("//div[normalize-space(@class) = 'PageListStandardE']");
        foreach (var storySection in storySections)
        {
            // Find the main article in the story section
            var mainArticle = storySection.SelectSingleNode(".//div[normalize-space(@class) = 'PageListStandardE-leadPromo-info']");
            var mainArticleUrl = mainArticle.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
            var mainHeadline = mainArticle.SelectSingleNode(".//span").InnerText;
            if (!mainArticleUrl.StartsWith($"{baseUrl}/article") && !mainArticleUrl.StartsWith($"{baseUrl}/live"))
                continue;
            logger.Log("Main Article");
            logger.Log($"  Headline: {mainHeadline}", logAsRawMessage: true);
            logger.Log($"  Url: {mainArticleUrl}", logAsRawMessage: true);
            //logger.Log("----------------------------------------------", logAsRawMessage: true);

            var secondaryArticles = storySection.SelectSingleNode(".//div[normalize-space(@class) = 'PageListStandardE-items-secondary']");
            if (secondaryArticles is null)
                continue;

            foreach (var secondaryArticle in secondaryArticles.SelectNodes(".//div[normalize-space(@class) = 'PagePromo']"))
            {
                var articleUrl = secondaryArticle.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
                var headline = secondaryArticle.SelectSingleNode(".//span").InnerText;
                var articleUnixTimestamp = secondaryArticle.SelectSingleNode(".//bsp-timestamp[@data-timestamp]")?.GetAttributeValue("data-timestamp", "");
                logger.Log($"Secondary Article", logAsRawMessage: true);
                logger.Log($"  Headline: {headline}", logAsRawMessage: true);
                logger.Log($"  Url: {articleUrl}", logAsRawMessage: true);

                //logger.Log($"articleUnixTimestamp = {articleUnixTimestamp}");
                if (long.TryParse(articleUnixTimestamp, out long timestamp))
                {
                    DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
                    DateTime dateTime = dateTimeOffset.LocalDateTime;
                    logger.Log($"  Last Updated (Local): {dateTime}");
                }
            }
            logger.Log("----------------------------------------------", logAsRawMessage: true);
        }
    }

    public async Task<List<SourceArticle>?> GetArticles()
    {
        string baseUrl = "https://apnews.com";
        string articlesPrefix = $"{baseUrl}/article/";
        string livePrefix = $"{baseUrl}/live/";

        HtmlDocument htmlDoc = new();
        //htmlDoc.LoadHtml(await new HttpClient().GetStringAsync(baseUrl));
        htmlDoc.Load(@"C:/Users/danny/OneDrive/Projects/SnapshotNewsToday/TestData/AssociatedPressNews.html");

        // Get Main Story Articles
        //GetMainStoryArticles(htmlDoc);

        // Get news articles on right side (C block)
        var cBlockGrouping = htmlDoc.DocumentNode.SelectSingleNode("//div[normalize-space(@class) = 'PageListRightRailA' and @data-tb-region='C block']");
        var firstOtherArticle = cBlockGrouping.SelectSingleNode("//div[normalize-space(@class) = 'PageList-items-first']");
        var articleUrl = firstOtherArticle.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
        var headline = firstOtherArticle.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText;
        var articleUnixTimestamp = firstOtherArticle.SelectSingleNode(".//bsp-timestamp[@data-timestamp]")?.GetAttributeValue("data-timestamp", "");
        logger.Log($"First Other Article", logAsRawMessage: true);
        logger.Log($"  Headline: {headline}", logAsRawMessage: true);
        logger.Log($"  Url: {articleUrl}", logAsRawMessage: true);
        //logger.Log($"  articleUnixTimestamp = {articleUnixTimestamp}");
        if (long.TryParse(articleUnixTimestamp, out long timestamp))
        {
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
            DateTime dateTime = dateTimeOffset.LocalDateTime;
            logger.Log($"  Last Updated (Local): {dateTime}");
        }

        // PageList-items-item
        var otherArticles = cBlockGrouping.SelectNodes(".//li[normalize-space(@class) = 'PageList-items-item']");
        //var otherArticles = cBlockGrouping.SelectNodes(".//div[normalize-space(@class) = 'PageListRightRailA-content']");
        int count = 0;
        foreach (var otherArticle in otherArticles)
        {
            var otherArticleUrl = otherArticle.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
            var otherHeadline = otherArticle.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText;
            //var otherArticleUnixTimestamp = otherArticle.SelectSingleNode(".//bsp-timestamp[@data-timestamp]")?.GetAttributeValue("data-timestamp", "");
            var otherArticleUnixTimestamp = otherArticle.SelectSingleNode(".//div[normalize-space(@class) = 'PagePromo']")?.GetAttributeValue("data-updated-date-timestamp", "");
            logger.Log($"Other Article {++count}", logAsRawMessage: true);
            logger.Log($"  Headline: {otherHeadline}", logAsRawMessage: true);
            logger.Log($"  Url: {otherArticleUrl}", logAsRawMessage: true);
            logger.Log($"  ArticleUnixTimestamp = {otherArticleUnixTimestamp}, logAsRawMessage: true");
            if (long.TryParse(otherArticleUnixTimestamp, out long otherArticleTimestamp))
            {
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(otherArticleTimestamp);
                DateTime dateTime = dateTimeOffset.LocalDateTime;
                logger.Log($"  Last Updated (Local): {dateTime}");
            }
        }

        // Get Horizontal News Articles (List B)
        var listBGrouping = htmlDoc.DocumentNode.SelectSingleNode("//bsp-list-loadmore[normalize-space(@class) = 'PageListStandardB' and @data-gtm-modulestyle='List B']");
        //logger.Log($"{TrimInnerHtmlWhitespace(listBGrouping.OuterHtml)}", logAsRawMessage: true);
        var listBArticles = listBGrouping.SelectNodes(".//div[normalize-space(@class) = 'PageList-items-item']");
        int listBCount = 0;
        foreach(var listBArticle in listBArticles)
        {
            var otherArticleUrl = listBArticle.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
            var otherHeadline = listBArticle.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText;
            //var otherArticleUnixTimestamp = listBArticle.SelectSingleNode(".//bsp-timestamp[@data-timestamp]")?.GetAttributeValue("data-timestamp", "");
            var otherArticleUnixTimestamp = listBArticle.SelectSingleNode(".//div[normalize-space(@class) = 'PagePromo']")?.GetAttributeValue("data-updated-date-timestamp", "");
            logger.Log($"Other Article {++listBCount}", logAsRawMessage: true);
            logger.Log($"  Headline: {otherHeadline}", logAsRawMessage: true);
            logger.Log($"  Url: {otherArticleUrl}", logAsRawMessage: true);
            logger.Log($"  ArticleUnixTimestamp = {otherArticleUnixTimestamp}", logAsRawMessage: true);
            if (long.TryParse(otherArticleUnixTimestamp, out long otherArticleTimestamp))
            {
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(otherArticleTimestamp);
                DateTime dateTime = dateTimeOffset.LocalDateTime;
                logger.Log($"  Last Updated (Local): {dateTime}");
            }
        }

        // Get Most Read (These can be duplicates of other articles but it's good to mark as "Most Read"
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
            logger.Log($"Other Article {++mostReadCount}", logAsRawMessage: true);
            logger.Log($"  Headline: {otherHeadline}", logAsRawMessage: true);
            logger.Log($"  Url: {otherArticleUrl}", logAsRawMessage: true);
            logger.Log($"  ArticleUnixTimestamp = {otherArticleUnixTimestamp}", logAsRawMessage: true);
            if (long.TryParse(otherArticleUnixTimestamp, out long otherArticleTimestamp))
            {
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(otherArticleTimestamp);
                DateTime dateTime = dateTimeOffset.LocalDateTime;
                logger.Log($"  Last Updated (Local): {dateTime}");
            }
        }

        // B Block (Latest Published) Articles
        var bBlockLatestPublishedGrouping = htmlDoc.DocumentNode.SelectSingleNode("//div[normalize-space(@class) = 'PageListRightRailA' and @data-tb-region='B2']");
        var bBlockLatestPublishedArticles = bBlockLatestPublishedGrouping.SelectNodes(".//div[normalize-space(@class) = 'PagePromo']");
        int bBlockLatestPublishedCount = 0;
        foreach (var bBlockLatestPublishedArticle in bBlockLatestPublishedArticles)
        {
            var otherArticleUrl = bBlockLatestPublishedArticle.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
            var otherHeadline = bBlockLatestPublishedArticle.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText;
            //var otherArticleUnixTimestamp = listBArticle.SelectSingleNode(".//bsp-timestamp[@data-timestamp]")?.GetAttributeValue("data-timestamp", "");
            var otherArticleUnixTimestamp = bBlockLatestPublishedArticle.GetAttributeValue("data-updated-date-timestamp", "");
            logger.Log($"Other Article {++bBlockLatestPublishedCount}", logAsRawMessage: true);
            logger.Log($"  Headline: {otherHeadline}", logAsRawMessage: true);
            logger.Log($"  Url: {otherArticleUrl}", logAsRawMessage: true);
            logger.Log($"  ArticleUnixTimestamp = {otherArticleUnixTimestamp}", logAsRawMessage: true);
            if (long.TryParse(otherArticleUnixTimestamp, out long otherArticleTimestamp))
            {
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(otherArticleTimestamp);
                DateTime dateTime = dateTimeOffset.LocalDateTime;
                logger.Log($"  Last Updated (Local): {dateTime}");
            }
        }

        // In Case You Missed It (ICYMI) Articles
        var icymiGrouping = htmlDoc.DocumentNode.SelectSingleNode("//div[@data-tb-region='ICYMI']");
        //logger.Log($"{TrimInnerHtmlWhitespace(icymiGrouping.OuterHtml)}", logAsRawMessage: true);
        var icymiArticles = icymiGrouping.SelectNodes(".//div[normalize-space(@class) = 'PagePromo']");
        int icymiCount = 0;
        foreach (var icymiArticle in icymiArticles)
        {
            var otherArticleUrl = icymiArticle.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
            var otherHeadline = icymiArticle.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText;
            //var otherArticleUnixTimestamp = listBArticle.SelectSingleNode(".//bsp-timestamp[@data-timestamp]")?.GetAttributeValue("data-timestamp", "");
            var otherArticleUnixTimestamp = icymiArticle.GetAttributeValue("data-updated-date-timestamp", "");
            logger.Log($"ICYMI Article {++icymiCount}", logAsRawMessage: true);
            logger.Log($"  Headline: {otherHeadline}", logAsRawMessage: true);
            logger.Log($"  Url: {otherArticleUrl}", logAsRawMessage: true);
            logger.Log($"  ArticleUnixTimestamp = {otherArticleUnixTimestamp}", logAsRawMessage: true);
            if (long.TryParse(otherArticleUnixTimestamp, out long otherArticleTimestamp))
            {
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(otherArticleTimestamp);
                DateTime dateTime = dateTimeOffset.LocalDateTime;
                logger.Log($"  Last Updated (Local): {dateTime}");
            }
        }

        // Be Well Articles
        var beWellGrouping = htmlDoc.DocumentNode.SelectSingleNode("//div[@data-gtm-region='be well headline queue']");
        var beWellArticles = beWellGrouping.SelectNodes(".//div[normalize-space(@class) = 'PagePromo']");
        int beWellCount = 0;
        foreach (var beWellArticle in beWellArticles)
        {
            var otherArticleUrl = beWellArticle.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
            var otherHeadline = beWellArticle.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText;
            //var otherArticleUnixTimestamp = listBArticle.SelectSingleNode(".//bsp-timestamp[@data-timestamp]")?.GetAttributeValue("data-timestamp", "");
            var otherArticleUnixTimestamp = beWellArticle.GetAttributeValue("data-updated-date-timestamp", "");
            logger.Log($"Be Well Article {++beWellCount}", logAsRawMessage: true);
            logger.Log($"  Headline: {otherHeadline}", logAsRawMessage: true);
            logger.Log($"  Url: {otherArticleUrl}", logAsRawMessage: true);
            logger.Log($"  ArticleUnixTimestamp = {otherArticleUnixTimestamp}", logAsRawMessage: true);
            if (long.TryParse(otherArticleUnixTimestamp, out long otherArticleTimestamp))
            {
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(otherArticleTimestamp);
                DateTime dateTime = dateTimeOffset.LocalDateTime;
                logger.Log($"  Last Updated (Local): {dateTime}");
            }
        }

        // US News 
        var usNewsGrouping = htmlDoc.DocumentNode.SelectSingleNode("//div[@data-gtm-topic='Topics - US News']");
        var usNewsArticles = usNewsGrouping.SelectNodes(".//div[normalize-space(@class) = 'PagePromo']");
        int usNewsCount = 0;
        foreach (var usNewsArticle in usNewsArticles)
        {
            var otherArticleUrl = usNewsArticle.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
            var otherHeadline = usNewsArticle.SelectSingleNode(".//span[normalize-space(@class) = 'PagePromoContentIcons-text']").InnerText;
            //var otherArticleUnixTimestamp = listBArticle.SelectSingleNode(".//bsp-timestamp[@data-timestamp]")?.GetAttributeValue("data-timestamp", "");
            var otherArticleUnixTimestamp = usNewsArticle.GetAttributeValue("data-updated-date-timestamp", "");
            logger.Log($"US News Article {++usNewsCount}", logAsRawMessage: true);
            logger.Log($"  Headline: {otherHeadline}", logAsRawMessage: true);
            logger.Log($"  Url: {otherArticleUrl}", logAsRawMessage: true);
            logger.Log($"  ArticleUnixTimestamp = {otherArticleUnixTimestamp}", logAsRawMessage: true);
            if (long.TryParse(otherArticleUnixTimestamp, out long otherArticleTimestamp))
            {
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(otherArticleTimestamp);
                DateTime dateTime = dateTimeOffset.LocalDateTime;
                logger.Log($"  Last Updated (Local): {dateTime}");
            }
        }

        // World News (AP News mislabels this section as "Topics - Sports")
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
            //var otherArticleUnixTimestamp = listBArticle.SelectSingleNode(".//bsp-timestamp[@data-timestamp]")?.GetAttributeValue("data-timestamp", "");
            var otherArticleUnixTimestamp = worldNewsArticle.GetAttributeValue("data-updated-date-timestamp", "");
            logger.Log($"World News Article {++worldNewsCount}", logAsRawMessage: true);
            logger.Log($"  Headline: {otherHeadline}", logAsRawMessage: true);
            logger.Log($"  Url: {otherArticleUrl}", logAsRawMessage: true);
            logger.Log($"  ArticleUnixTimestamp = {otherArticleUnixTimestamp}", logAsRawMessage: true);
            if (long.TryParse(otherArticleUnixTimestamp, out long otherArticleTimestamp))
            {
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(otherArticleTimestamp);
                DateTime dateTime = dateTimeOffset.LocalDateTime;
                logger.Log($"  Last Updated (Local): {dateTime}");
            }
        }

        // Politics


        // Entertainment


        // Sports
        // NOTE: AP News has "Sports" as "Topics - World News" so we'll use data-module-number="10.1"


        // Business


        // Science


        // Lifestyle



        // Technology



        // Health



        // Climate


        // Fact Check


        // Latest News







        //logger.Log($"Article groupings = {articleGrouping.Count}", logAsRawMessage: true);
        //foreach (var articleGroup in articleGrouping)
        //{

        //}
        // PageListRightRailA-content
        // PageList-items
        // PageList-items-item
        // PagePromo-content
        //var otherNewsArticles = cBlockGrouping.SelectNodes(".//div[normalize-space(@class) = 'PageListRightRailA-content']");
        //var otherNewsArticles = otherNewsArticlesSection.SelectNodes("//div[normalize-space(@class) = 'PageList-items-item']");


        return null;

        //// Find the first story section div
        //var storySection2 = htmlDoc.DocumentNode.SelectSingleNode("//div[normalize-space(@class) = 'PageListStandardE']");
        ////logger.Log($"{TrimInnerHtmlWhitespace(storySection.OuterHtml)}", LogLevel.Info, logAsRawMessage: true);

        //// Find the main article in the story section
        //var mainArticle2 = storySection2.SelectSingleNode(".//div[normalize-space(@class) = 'PageListStandardE-leadPromo-info']");
        //logger.Log("----------------------------------------------", logAsRawMessage: true);
        ////logger.Log($"{TrimInnerHtmlWhitespace(mainArticle.OuterHtml)}", LogLevel.Info, logAsRawMessage: true);
        ////var ahref = mainArticle.SelectSingleNode(".//a[@href]");
        //var mainArticleUrl2 = mainArticle2.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
        //var mainHeadline2 = mainArticle2.SelectSingleNode(".//span").InnerText;
        //logger.Log($"articleUrl = {mainArticleUrl2}");
        //logger.Log($"headline = {mainHeadline2}");
        //logger.Log("----------------------------------------------", logAsRawMessage: true);
        
        //// Find all the secondary articles in the story section
        //var secondaryArticles2 = storySection2.SelectSingleNode(".//div[normalize-space(@class) = 'PageListStandardE-items-secondary']");
        ////logger.Log($"{TrimInnerHtmlWhitespace(secondaryArticles.OuterHtml)}", LogLevel.Info, logAsRawMessage: true);
        //foreach (var secondaryArticle in secondaryArticles2.SelectNodes(".//div[normalize-space(@class) = 'PagePromo']"))
        //{
        //    //logger.Log($"{TrimInnerHtmlWhitespace(secondaryArticle.InnerHtml)}", LogLevel.Info, logAsRawMessage: true);
        //    var articleUrl = secondaryArticle.SelectSingleNode(".//a[@href]").GetAttributeValue("href", "");
        //    var headline = secondaryArticle.SelectSingleNode(".//span").InnerText;
        //    // <bsp-timestamp data-timestamp="1760377076000" data-recent-thresholdinhours="1">
        //    var articleUnixTimestamp = secondaryArticle.SelectSingleNode(".//bsp-timestamp[@data-timestamp]").GetAttributeValue("data-timestamp", "");
        //    logger.Log($"articleUrl = {articleUrl}");
        //    logger.Log($"headline = {headline}");
        //    logger.Log($"articleUnixTimestamp = {articleUnixTimestamp}");
        //    if (long.TryParse(articleUnixTimestamp, out long timestamp))
        //    {
        //        DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
        //        DateTime dateTime = dateTimeOffset.LocalDateTime;
        //        logger.Log($"Article DateTime (Local): {dateTime}");
        //    }
        //    logger.Log("----------------------------------------------", logAsRawMessage: true);
        //}

        return null;

        //var links = htmlDoc.DocumentNode
        //    .SelectNodes("//a[@href]")
        //    ?.Select(node => node.GetAttributeValue("href", ""))
        //    .Where(href =>
        //        href.StartsWith(articlesPrefix, StringComparison.OrdinalIgnoreCase) ||
        //        href.StartsWith(livePrefix, StringComparison.OrdinalIgnoreCase))
        //    .Distinct()
        //    .ToList() ?? new List<string>();

        //logger.Log($"Total links found on {baseUrl}: {links.Count}", LogLevel.Info);
        //foreach (var link in links) 
        //{             
        //    logger.Log($"Found link: {link}", LogLevel.Info);
        //}

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

        return null;
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
