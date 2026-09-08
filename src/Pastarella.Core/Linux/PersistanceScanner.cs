using Pastarella.Core.Linux.PersistenceScanners;
using Pastarella.Core.Models;

namespace Pastarella.Core.Linux;

public class PersistenceScanner : IPersistenceScanner
{
    public IEnumerable<PersistenceEntry> Scan(IProgress<ScanProgress>? progress = null)
    {
        var lkml = new LKMScanner();
        var xdgAutostart = new XdgAutostart();

        progress?.Report(new ScanProgress(0, 2, "kernel modules"));
        var first = lkml.Scan(progress).ToList();
        progress?.Report(new ScanProgress(1, 2, "xdg autostart"));
        var second = xdgAutostart.Scan(progress).ToList();
        progress?.Report(new ScanProgress(2, 2));

        return first.Concat(second);
    }
}
