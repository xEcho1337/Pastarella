namespace Pastarella.Core.Models;

public record ProcessInfo(int Id)
{
    public required ExePath ExePath { get; init; }
    public string? CommandArgs { get; init; }

    public DateTime? StartTime { get; init; }

    public Dictionary<string, object> Metadata { get; init; } = [];
}

public interface IProcessScanner
{
    IEnumerable<ProcessInfo> Scan(IProgress<ScanProgress>? progress = null);
}
