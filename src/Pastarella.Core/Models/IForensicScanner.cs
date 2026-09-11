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

public record UserInfo(string Name, string Description, string Uid, string Home, bool Disabled)
{
    public Dictionary<string, object> Metadata { get; init; } = [];
}

public record StorageInfo(DriveType Type, string Name, ulong FreeSpace, ulong TotalSpace);

public interface IForensicScanner
{
    IEnumerable<ProcessInfo> ScanProcesses(IProgress<ScanProgress>? progress = null);
    IEnumerable<UserInfo> ScanUsers(IProgress<ScanProgress>? progress = null);

    IEnumerable<StorageInfo> ScanStorages()
    {
        if (Context.Os == Context.OS.Windows)
        {
            return Windows.ForensicScanners.Storages.Scan();
        }
        else
        {
            return DriveInfo.GetDrives().Select(d =>
                new StorageInfo(
                    d.DriveType,
                    d.Name,
                    d.IsReady ? (ulong)d.TotalFreeSpace : 0,
                    d.IsReady ? (ulong)d.TotalSize : 0)
            );
        }
    }
}
