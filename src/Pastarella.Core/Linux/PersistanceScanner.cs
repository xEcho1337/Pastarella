using Pastarella.Core.Linux.PersistenceScanners;
using Pastarella.Core.Models;

namespace Pastarella.Core.Linux;

public class PersistenceScanner : IPersistenceScanner
{
    public IEnumerable<PersistenceEntry> Scan()
    {
        var lkml = new LKMScanner();
        var xdgAutostart = new XdgAutostart();

        return lkml.Scan()
            .Concat(xdgAutostart.Scan());
    }
}
