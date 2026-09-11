using Pastarella.Core.Models;

namespace Pastarella.Core.Common;

public class GenericStorageScanner : IStorageScanner
{
    public IEnumerable<StorageInfo> Scan(IProgress<ScanProgress>? progress = null)
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

