using Pastarella.Core.Linux.PersistenceScanners;
using Pastarella.Core.Models;

namespace Pastarella.Core.Linux;

public class DriverScanner : IDriverScanner
{
    public static IEnumerable<DriverInfo> GetLoadedModules()
    {
        List<DriverInfo> list = [];

        foreach (string line in File.ReadAllLines("/proc/modules"))
        {
            string[] parts = line.Split(' ', 6);

            string name = parts[0];
            bool loaded = parts[2] != "0";
            string state = parts[4];

            string? filePath = LKMScanner.FindModulePath(name);
            string hash = PlatformHelpers.GetSha256(filePath);

            list.Add(new(
                name,
                "",
                name,
                DriverType.KernelModule,
                filePath,
                null,
                loaded,
                hash,
                null
            ));
        }

        return list;
    }

    public static IEnumerable<DriverInfo> GetBuiltinModules()
    {
        List<DriverInfo> list = [];

        string modulesPath = $"{AnalysisDispatcher.ModulesPath}/{AnalysisDispatcher.GetKernelVersion()}";
        string kernelFilePath = $"{modulesPath}/vmlinuz";
        string? kernelSha256 = PlatformHelpers.GetSha256(kernelFilePath);

        string? previousModule = null;
        string displayName = "";
        string? identifier = null;
        string? version = null;

        string text = File.ReadAllText($"{modulesPath}/modules.builtin.modinfo");
        foreach (string line in text.Split('\0', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.StartsWith(' ') || line.StartsWith('\t'))
                continue;

            string[] kv = line.Split('=', 2);
            string[] keyParts = kv[0].Split('.', 2);
            string mod = keyParts[0];
            if (previousModule is string prev && prev != mod)
            {
                list.Add(new(
                    previousModule,
                    displayName,
                    identifier ?? previousModule,
                    DriverType.BuiltinKernelModule,
                    kernelFilePath,
                    version,
                    true,
                    kernelSha256,
                    null
                ));

                displayName = "";
                identifier = null;
                version = null;

                previousModule = mod;
            }
            previousModule ??= mod;

            string val = kv[1].TrimEnd();
            switch (keyParts[1])
            {
                case "description":
                    displayName = val;
                    break;
                case "version":
                    version = val;
                    break;
                case "alias":
                    if (identifier is string id)
                        identifier = $"{id} {val}";
                    else
                        identifier = val;
                    break;
            }
        }

        return list;
    }

    public IEnumerable<DriverInfo> Scan()
    {
        return GetLoadedModules()
            .Concat(GetBuiltinModules());
    }
}

