namespace Pastarella.Core.Linux;

public class Context
{
    public bool UsrMerged { get; private set; }
    public string ModulesPath { get; private set; } = "/lib/modules";

    public Context()
    {
        if (new DirectoryInfo("/lib").LinkTarget is string target)
            UsrMerged = target == "usr/lib";

        if (UsrMerged)
            ModulesPath = "/usr/lib/modules";
    }

    public static string GetKernelVersion()
    {
        // TODO: get version via uname() libc function
        string[] parts = File.ReadAllLines("/proc/version")[0].Split(' ');
        return parts[2];
    }

    public string? FindModulePath(string moduleName)
    {
        string modulesPath = $"{ModulesPath}/{GetKernelVersion()}";
        foreach (string line in File.ReadLines($"{modulesPath}/modules.dep"))
        {
            string path = line.Split(':')[0];
            if (path.Split('/')[^1].Split('.')[0] == moduleName)
                return $"{modulesPath}/{path}";
        }

        return null;
    }

}
