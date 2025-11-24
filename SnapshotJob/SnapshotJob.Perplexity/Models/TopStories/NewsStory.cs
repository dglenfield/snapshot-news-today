using SnapshotNewsToday.Common.Serialization;
using System.Text.Json;

namespace SnapshotJob.Perplexity.Models.TopStories;

public class NewsStory
{
    public string Id { get; set; } = default!;
    public string Headline { get; set; } = default!;

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default, 
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);

}
