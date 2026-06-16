using Pastarella.Core.FreeBSD.PersistenceScanners;
using Pastarella.Core.Models;

namespace Pastarella.Core.FreeBSD;

public class PersistenceScanner : IPersistenceScanner
{
    public IEnumerable<PersistenceEntry> Scan()
    {
        return new LKMScanner().Scan();
    }
}
