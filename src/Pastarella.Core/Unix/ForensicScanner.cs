using Pastarella.Core.Models;

namespace Pastarella.Core.Unix;

public class ForensicScanner : IForensicScanner
{
    public IEnumerable<ProcessInfo> ScanProcesses(IProgress<ScanProgress>? progress = null)
    {
        throw new Exception("Scanning processes is OS-specific, not a UNIX \"standard\"");
    }

    public IEnumerable<UserInfo> ScanUsers(IProgress<ScanProgress>? progress = null)
    {
        return ForensicScanners.Users.Scan();
    }
}

