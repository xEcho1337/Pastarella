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

    public LKMScanner()
    {
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

        bool microcodeEnabled = false;
        string? microcodePath = null;
        string? microcodeDeclaredOn = null;

        foreach (string configFile in BootloaderConfigFiles)
        {
            if (!File.Exists(configFile))
                continue;

            foreach (string line in File.ReadAllLines(configFile))
            {
                if (string.IsNullOrWhiteSpace(line) || line[0] == '#')
                    continue;

                if (line.EndsWith("_load=\"YES\""))
                {
                    if (line.StartsWith("cpu_microcode"))
                    {
                        microcodeEnabled = true;
                        continue;
                    }

                    string moduleName = line[..^"_load=\"YES\"".Length];
                    string modulePath = $"/boot/kernel/{moduleName}.ko";

                    list.Add(new PersistenceEntry()
                    {
                        Name = moduleName,
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
                else if (line.StartsWith("cpu_microcode_name"))
                {
                    microcodePath = line["cpu_microcode_name=\"".Length..^1];
                    microcodeDeclaredOn = configFile;
                }
            }
        }

        if (microcodeEnabled)
        {
            list.Add(new PersistenceEntry()
            {
                Name = "cpu_microcode",
                Path = microcodeDeclaredOn!,
                Action = new ExecScheduledAction
                {
                    ExePath = new(microcodePath!, true),
                },
                Type = PersistenceType.Firmware,
                Trigger = ExecutionTrigger.Boot,
                Privilege = PersistencePrivilege.Kernel,
            });
        }

        return list;
    }
}
