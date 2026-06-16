using System.Diagnostics;
using Pastarella.Core.Models;

namespace Pastarella.Core.Linux;

public class ForensicScanner : IForensicScanner
{
    public static IEnumerable<UserInfo> CachedUsersInfo
    {
        get => field ??= new Unix.ForensicScanner().ScanUsers();
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
