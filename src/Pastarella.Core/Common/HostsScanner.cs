using Pastarella.Core.Models;

namespace Pastarella.Core.Common;

public static class HostsScanner
{
    public static readonly string Path = OperatingSystem.IsWindows() ? @"C:\Windows\System32\drivers\etc\hosts" : "/etc/hosts";

    public static IEnumerable<Host> GetHosts()
    {
        var hosts = new List<Host>();

        foreach (string line in File.ReadLines(Path))
        {
            if (line.Equals(Environment.NewLine) || line.StartsWith('#'))
                continue;

            string[] split = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);

            if (split.Length > 1)
                hosts.Add(new Host(split[0], split[1]));
        }

        return hosts;
    }
}
