using SnapshotJob.Common.Serialization;
using SnapshotJob.Perplexity.Models.TopStories.Response;
using System.Text.Json;

namespace SnapshotJob.Perplexity.Models.AnalyzeArticle;

public class AnalyzeArticleResult
{
    public string? RequestBody { get; set; }
    public string? ResponseString { get; set; }
    public Exception? Exception { get; set; }

    // PerplexityResponse
    public string PerplexityResponseId { get; set; } = default!;
    public string PerplexityResponseModel { get; set; } = default!;
    public Usage PerplexityApiUsage { get; set; } = default!;
    public List<string> Citations { get; set; } = default!;
    public List<SearchResult> SearchResults { get; set; } = default!;

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
