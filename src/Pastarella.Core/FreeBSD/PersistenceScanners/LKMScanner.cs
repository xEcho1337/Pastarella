using Pastarella.Core.Models;

namespace Pastarella.Core.FreeBSD.PersistenceScanners;

public class LKMScanner : IPersistenceScanner
{
    public IEnumerable<PersistenceEntry> Scan()
    {
        List<PersistenceEntry> list = [];

        var lines = File.ReadAllLines("/boot/loader.conf")
                .Where(l => l.EndsWith("_load=\"YES\""))
                .Select(l => l[..^"_load=\"YES\"".Length]);

        foreach (string line in lines)
        {
            string modulePath = $"/boot/kernel/{line}.ko";

            list.Add(new PersistenceEntry()
            {
                Name = line,
                Path = "/boot/loader.conf",
                Action = new ExecScheduledAction
                {
                    Path = modulePath,
                    Sha256 = PlatformHelpers.GetSha256(modulePath),
                },

                Type = PersistenceType.LoadableKernelModule,
                Trigger = ExecutionTrigger.Boot,
                Privilege = PersistencePrivilege.Kernel,
            });
        }

        return list;
    }
}
