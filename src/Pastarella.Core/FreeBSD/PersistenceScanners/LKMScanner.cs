using Pastarella.Core.Models;

namespace Pastarella.Core.FreeBSD.PersistenceScanners;

public class LKMScanner : IPersistenceScanner
{
    private readonly string[] BootloaderGeneralConfigFiles = [
        "/boot/defaults/loader.conf",
        "/boot/loader.conf",
        "/boot/loader.conf.local",
    ];

    private string[] BootloaderConfigFiles;

    public LKMScanner() {
        // TODO: handle lua configuration files

        // TODO: we should parse `loader_config_dirs` for getting this
        string[] loaderConfDFiles = Directory.EnumerateFiles("/boot/loader.conf.d").ToArray();

        // TODO: some of these files can override each other. We should handle this.
        BootloaderConfigFiles = new string[BootloaderGeneralConfigFiles.Length + loaderConfDFiles.Count()];
        Array.Copy(BootloaderGeneralConfigFiles, 0, BootloaderConfigFiles, 0, BootloaderGeneralConfigFiles.Length);
        Array.Copy(loaderConfDFiles, 0, BootloaderConfigFiles, BootloaderGeneralConfigFiles.Length, loaderConfDFiles.Count());
    }

    public IEnumerable<PersistenceEntry> Scan(IProgress<ScanProgress>? progress = null)
    {
        List<PersistenceEntry> list = [];

        foreach (string configFile in BootloaderConfigFiles) {
            if (!File.Exists(configFile))
                continue;

            var lines = File.ReadAllLines(configFile)
                    .Where(l => !l.StartsWith('#') && l.EndsWith("_load=\"YES\""))
                    .Select(l => l[..^"_load=\"YES\"".Length]);

            foreach (string line in lines)
            {
                string modulePath = $"/boot/kernel/{line}.ko";

                list.Add(new PersistenceEntry()
                {
                    Name = line,
                    Path = configFile,
                    Action = new ExecScheduledAction
                    {
                        ExePath = new(modulePath, true),
                    },
                    Type = PersistenceType.LoadableKernelModule,
                    Trigger = ExecutionTrigger.Boot,
                    Privilege = PersistencePrivilege.Kernel,
                });
            }
        }

        return list;
    }
}
