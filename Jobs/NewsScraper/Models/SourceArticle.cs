using System.Text;
using System.Text.Json.Serialization;

namespace NewsScraper.Models;

/// <summary>
/// Represents a news article and its content.
/// </summary>
public class SourceArticle
{
    public Uri ArticleUri { get; set; } = default!;
    public string? Author { get; set; }
    public List<string> ContentParagraphs { get; set; } = [];
    public string? ErrorMessage { get; set; }
    public string? Headline { get; set; }
    public bool? IsPaywalled { get; set; }
    public DateTime? LastUpdatedDate { get; set; }
    public DateTime? PublishDate { get; set; }
    public bool? Success { get; set; }

    [JsonIgnore]
    public string Content => string.Join("\n\n", ContentParagraphs);

    public string GetContentParagraphs(int maxParagraphs)
    {
        if (maxParagraphs <= 0 || ContentParagraphs.Count == 0)
            return string.Empty;

        StringBuilder builder = new();
        for (int i = 0; i < maxParagraphs && i < ContentParagraphs.Count; i++)
            builder.AppendLine($"  [{i + 1}] {ContentParagraphs[i]}");

        return builder.ToString();
    }
}
