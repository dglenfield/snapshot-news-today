using Common.Serialization;
using System.Text.Json;

namespace Common.Models.AssociatedPress.MainPage;

public class APNewsHeadline
{
    public long Id { get; set; }
    public string? SectionName { get; init; }
    public string? Title { get; set; } // Headline
    public Uri TargetUri { get; set; } = default!;
    public DateTime? LastUpdatedOn { get; set; } // UTC time
    public bool MostRead { get; set; } = false;
    public bool AlreadyInDatabase { get; set; } = false;

    public override bool Equals(object? obj)
    {
        return obj is APNewsHeadline other && TargetUri?.Equals(other.TargetUri) == true;
    }

    public override int GetHashCode()
    {
        return TargetUri?.GetHashCode() ?? 0;
    }

    public override string ToString() => JsonConfig.ToJson(this, JsonSerializerOptions.Default,
        CustomJsonSerializerOptions.IgnoreNull | CustomJsonSerializerOptions.WriteIndented);
}
