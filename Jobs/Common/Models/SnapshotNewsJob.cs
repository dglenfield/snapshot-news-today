namespace Common.Models;

public class SnapshotNewsJob
{
    public long Id { get; set; }
    public DateTime JobStartedOn { get; } = DateTime.UtcNow;
    public DateTime? JobFinishedOn { get; set; }
    public bool? IsSuccess { get; set; }
}
