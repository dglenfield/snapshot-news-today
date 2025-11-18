using SnapshotJob.Common.Serialization;
using System.Text.Json;

namespace SnapshotJob.Perplexity.Models.TopStories;

public class StoryHeadline
{
    public required long Id { get; set; }
    public required string Headline { get; set; }

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
