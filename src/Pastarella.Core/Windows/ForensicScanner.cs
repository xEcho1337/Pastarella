using Pastarella.Core.Models;

namespace Pastarella.Core.Windows;

public class ForensicScanner : IForensicScanner
{
    public IEnumerable<ProcessInfo> ScanProcesses(IProgress<ScanProgress>? progress = null) {
        return ForensicScanners.Processes.Scan();
    }

    public IEnumerable<UserInfo> ScanUsers(IProgress<ScanProgress>? progress = null)
    {
        return ForensicScanners.Users.Scan();
    }
}
