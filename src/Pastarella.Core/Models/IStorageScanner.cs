namespace Pastarella.Core.Models;

public record StorageInfo(DriveType Type, string Name, ulong FreeSpace, ulong TotalSpace);

public interface IStorageScanner
{
    IEnumerable<StorageInfo> Scan(IProgress<ScanProgress>? progress = null);
}
