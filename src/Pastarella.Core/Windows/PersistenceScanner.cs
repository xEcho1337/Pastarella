using Pastarella.Core.Models;
using Pastarella.Core.Windows.PersistenceScanners;

namespace Pastarella.Core.Windows;

public class PersistenceScanner : IPersistenceScanner
{
    public IEnumerable<PersistenceEntry> Scan(IProgress<ScanProgress>? progress = null)
    {
        var registry = new RegistryScanner();
        var users = new UsersLogonScript();
        var tasks = new TaskScanner();

        progress?.Report(new ScanProgress(0, 3, "registry"));
        var first = registry.Scan(progress).ToList();
        progress?.Report(new ScanProgress(1, 3, "logon scripts"));
        var second = users.Scan(progress).ToList();
        progress?.Report(new ScanProgress(2, 3, "scheduled tasks"));
        var third = tasks.Scan(progress).ToList();
        progress?.Report(new ScanProgress(3, 3));

        return first.Concat(second).Concat(third);
    }
}
