using NewsScraper.Providers;
using NewsScraper.Utilities;

namespace NewsScraper;

public class Program
{
    public static void Main(string[] args)
    {
        Logger.Log("********** Application started **********");

        try
        {
            // 1. Fetch news from CNN or use debug article
            NewsWebsite targetSite = NewsWebsite.CNN;
            var distinctUrls = NewsProvider.GetNews(targetSite);

            Logger.Log($"Total unique articles found: {distinctUrls?.Count}");
            foreach (var url in distinctUrls!)
                Logger.Log(url.AbsoluteUri);
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
        }

        Logger.Log("********** Exiting application **********");
    }
}
