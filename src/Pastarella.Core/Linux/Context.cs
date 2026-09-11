namespace Pastarella.Core.Linux;

public static class Context
{
    public static bool UsrMerged { get; private set; }
    public static string ModulesPath { get; private set; } = "/lib/modules";

    public static void Setup()
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
}
