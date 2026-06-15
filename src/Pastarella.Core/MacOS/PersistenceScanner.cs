using Pastarella.Core.MacOS.PersistenceScanners;
using Pastarella.Core.Models;

namespace Pastarella.Core.MacOS;

public class PersistenceScanner : IPersistenceScanner
{
    public IEnumerable<PersistenceEntry> Scan()
    {
        var cron = new CronScanner();
        var launchd = new LaunchdScanner();

        return cron.Scan().Concat(launchd.Scan());
    }
}
