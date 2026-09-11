namespace Pastarella.Core.Models;

public record ProcessInfo(int Id)
{
    public Dictionary<string, object> Metadata { get; init; } = [];

    public string? CommandArgs { get; init; }

    public string? Path { get; init; }
    public string? Sha256 { get; init; }
    public string? Signer { get; init; }
    public DateTime? StartTime { get; init; }
}

public interface IProcessScanner
{
    IEnumerable<ProcessInfo> Scan(IProgress<ScanProgress>? progress = null);
}

