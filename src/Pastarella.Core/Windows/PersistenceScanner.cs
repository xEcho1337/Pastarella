using Pastarella.Core.Models;
using Pastarella.Core.Windows.PersistenceScanners;

namespace Pastarella.Core.Windows;

public class PersistenceScanner : IPersistenceScanner
{
    public IEnumerable<PersistenceEntry> Scan()
    {
        var registry = new RegistryScanner();
        var users = new UsersLogonScript();
        var tasks = new TaskScanner();

        return registry.Scan()
            .Concat(users.Scan())
            .Concat(tasks.Scan());
    }
}
