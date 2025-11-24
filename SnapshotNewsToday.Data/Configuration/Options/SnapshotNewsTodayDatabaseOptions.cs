using System.ComponentModel.DataAnnotations;

namespace SnapshotNewsToday.Data.Configuration.Options;

public class SnapshotNewsTodayDatabaseOptions
{
    public const string SectionName = "SnapshotNewsTodayDatabase";

    [Required]
    public string AccountEndpoint { get; set; } = default!;

    [Required]
    public string AccountKey { get; set; } = default!;

    [Required]
    public string DatabaseId { get; set; } = default!;

    [Required]
    public bool DeleteExistingDatabase { get; set; }
}
