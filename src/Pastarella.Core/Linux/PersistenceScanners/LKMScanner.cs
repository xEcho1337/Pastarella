using Pastarella.Core.Models;

namespace Pastarella.Core.Linux.PersistenceScanners;

public class LKMScanner : IPersistenceScanner
{
    private List<PersistenceEntry> Entries { get; init; } = [];

    private readonly string[] ModulesLoadDirs = AnalysisDispatcher.UsrMerged
        ? ["/etc/modules-load.d/", "/usr/lib/modules-load.d/"]
        : ["/etc/modules-load.d/", "/lib/modules-load.d/", "/usr/lib/modules-load.d/"];

    private readonly string[] ModprobeDirs = AnalysisDispatcher.UsrMerged
        ? ["/etc/modprobe.d/", "/usr/lib/modprobe.d/"]
        : ["/etc/modprobe.d/", "/lib/modprobe.d/", "/usr/lib/modprobe.d/"];

    public IEnumerable<PersistenceEntry> Scan()
    {
        // TODO: improve performance. Store all modules to find in a list, then
        // call `FindModulesPath` which scans line by line `modules.dep`, ...
        // and it populates `Entries` with the newly retrived `.ko` path.

        // TODO: scan for every kernel version available in /lib/modules/...

        ScanModulesLoad();
        ScanModprobe();

        return Entries;
    }

    public static string? FindModulePath(string moduleName)
    {
        string modulesPath = $"{AnalysisDispatcher.ModulesPath}/{AnalysisDispatcher.GetKernelVersion()}";
        foreach (string line in File.ReadLines($"{modulesPath}/modules.dep"))
        {
            string path = line.Split(':')[0];
            if (path.Split('/')[^1].Split('.')[0] == moduleName)
                return $"{modulesPath}/{path}";
        }

        return null;
    }

    private void ScanModulesLoad()
    {
        foreach (string dir in ModulesLoadDirs)
        {
            foreach (string filePath in Directory.GetFiles(dir))
            {
                foreach (string line in File.ReadAllLines(filePath))
                {
                    if (string.IsNullOrEmpty(line) || line[0] == '#')
                        continue;

                    string? modulePath = FindModulePath(line);
                    Entries.Add(new()
                    {
                        Name = line,
                        Path = filePath,
                        Action = new ExecScheduledAction
                        {
                            Path = modulePath,
                            Sha256 = PlatformHelpers.GetSha256(modulePath),
                        },

                        Type = PersistenceType.LoadableKernelModule,
                        Trigger = ExecutionTrigger.SystemStartup,
                        Privilege = PersistencePrivilege.Kernel,
                    });
                }
            }
        }
    }

    private void ScanModprobe()
    {
        foreach (string dir in ModprobeDirs)
        {
            foreach (string filePath in Directory.GetFiles(dir))
            {
                foreach (string line in File.ReadAllLines(filePath))
                {
                    if (string.IsNullOrEmpty(line) || line[0] == '#')
                        continue;

                    string[] parts = line.Split(' ');

                    string cmd = parts[0];
                    string moduleName = parts[1];
                    string? shell = parts.Length > 2 ? parts[2] : null;

                    if (cmd != "install" || (shell != null && (shell.EndsWith("/bin/false") || shell.EndsWith("/bin/true"))))
                        continue;

                    string? modulePath = FindModulePath(moduleName);
                    Entries.Add(new()
                    {
                        Name = moduleName,
                        Path = filePath,
                        Action = new ExecScheduledAction
                        {
                            Path = modulePath,
                            Sha256 = PlatformHelpers.GetSha256(modulePath),
                        },

                        Type = PersistenceType.LoadableKernelModule,
                        Trigger = ExecutionTrigger.SystemStartup,
                        Privilege = PersistencePrivilege.Kernel,
                    });
                }
            }
        }
    }
}
