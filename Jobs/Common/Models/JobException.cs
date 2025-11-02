namespace Common.Models;

public class JobException
{
    public required string Source { get; set; }
    public Exception Exception { get; set; } = default!;

    public string Message => Exception.Message;
}
