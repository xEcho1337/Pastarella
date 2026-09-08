using Pastarella.Core.Models;

namespace Pastarella.Core.FreeBSD;

public class ForensicScanner : IForensicScanner
{
    public IEnumerable<ProcessInfo> ScanProcesses(IProgress<ScanProgress>? progress = null)

    {
        throw new NotImplementedException();
    }

    public IEnumerable<UserInfo> ScanUsers(IProgress<ScanProgress>? progress = null)
    {
        return new Unix.ForensicScanner().ScanUsers(progress);
    }
}
