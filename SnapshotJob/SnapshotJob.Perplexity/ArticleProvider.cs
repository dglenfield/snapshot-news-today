using System.Text.RegularExpressions;

namespace SnapshotJob.Perplexity;

public class ArticleProvider(IHttpClientFactory httpClientFactory)
{


    private string TrimInnerHtmlWhitespace(string html)
    {
        // Replace multiple whitespace (including newlines/tabs) with a single space
        return Regex.Replace(html, @"\s+", " ").Trim();
    }
}
