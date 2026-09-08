using Pastarella.Core.MacOS.PersistenceScanners;
using Pastarella.Core.Models;

namespace Pastarella.Core.MacOS;

public class PersistenceScanner : IPersistenceScanner
{
    public IEnumerable<PersistenceEntry> Scan(IProgress<ScanProgress>? progress = null)
    {
        var cron = new CronScanner();
        var launchd = new LaunchdScanner();

        progress?.Report(new ScanProgress(0, 2, "cron"));
        var first = cron.Scan(progress).ToList();
        progress?.Report(new ScanProgress(1, 2, "launchd"));
        var second = launchd.Scan(progress).ToList();
        progress?.Report(new ScanProgress(2, 2));

        return first.Concat(second);
    }
}
