using System.Diagnostics;
using Pastarella.Core.Models;

namespace Pastarella.Core.Linux;

public class ForensicScanner : IForensicScanner
{
    public static List<UserInfo> CachedUsersInfo
    {
        get
        {
            if (field == null)
            {
                field = [];

                foreach (string line in File.ReadAllLines("/etc/passwd"))
                {
                    string[] parts = line.Split(':');

                    string name = parts[0];
                    string uid = parts[2];
                    string gid = parts[3];
                    string gecos = parts[4];
                    string home = parts[5];
                    string shell = parts[6];

                    field.Add(new(name, "", uid, home, false /* TODO */)
                    {
                        Metadata =
                        {
                            ["gecos"] = gecos,
                            ["gid"] = gid,
                            ["shell"] = shell,
                        },
                    });
                }
            }

            return field;
        }
    }

    public IEnumerable<ProcessInfo> ScanProcesses()
    {
        var list = new List<ProcessInfo>();

        foreach (var proc in Process.GetProcesses())
        {
            string? path = PlatformHelpers.TryGet(() => proc.MainModule?.FileName);
            DateTime? start = PlatformHelpers.TryGet(() => proc.StartTime);

            string? hash = PlatformHelpers.GetSha256(path);

            list.Add(new ProcessInfo(proc.Id, proc.ProcessName, path, hash, null, start));
        }

        return list;
    }

    public IEnumerable<UserInfo> ScanUsers()
    {
        return CachedUsersInfo;
    }
}
