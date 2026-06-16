using Pastarella.Core.Models;

namespace Pastarella.Core.FreeBSD;

public class ForensicScanner : IForensicScanner
{
    public IEnumerable<ProcessInfo> ScanProcesses()

    {
        throw new NotImplementedException();
    }

    public IEnumerable<UserInfo> ScanUsers()
    {
        return new Unix.ForensicScanner().ScanUsers();
    }
}
