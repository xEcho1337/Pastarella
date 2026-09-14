using Pastarella.Core.Models;

namespace Pastarella.Core.Linux;

public class DriverScanner(Context ctx) : IDriverScanner
{
    public IEnumerable<DriverInfo> GetLoadedModules()
    {
        List<DriverInfo> list = [];

        foreach (string line in File.ReadAllLines("/proc/modules"))
        {
            string[] parts = line.Split(' ', 6);

            string name = parts[0];
            bool loaded = (parts[4] == "Live") || (parts[4] == "Loading");

            string? modulePath = ctx.FindModulePath(name);
            list.Add(new(
                name,
                "",
                name,
                DriverType.KernelModule,
                (modulePath != null)
                    ? new(modulePath, true)
                    : null,
                null,
                loaded
            ));
        }

        return list;
    }

    public IEnumerable<DriverInfo> GetBuiltinModules()
    {
        List<DriverInfo> list = [];

        string modulesPath = $"{ctx.ModulesPath}/{Context.GetKernelVersion()}";
        ExePath kernelImage = new($"{modulesPath}/vmlinuz");

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
                    kernelImage,
                    version,
                    true
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

    public IEnumerable<DriverInfo> Scan(IProgress<ScanProgress>? progress = null)
    {
        return GetLoadedModules()
            .Concat(GetBuiltinModules());
    }
}
