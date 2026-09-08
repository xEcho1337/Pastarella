using System.Diagnostics;
using Pastarella.Core.Models;

namespace Pastarella.Core.Linux;

public class ForensicScanner : IForensicScanner
{
    public static IEnumerable<UserInfo> CachedUsersInfo
    {
        get => field ??= new Unix.ForensicScanner().ScanUsers();
    }

    public IEnumerable<ProcessInfo> ScanProcesses(IProgress<ScanProgress>? progress = null)
    {
        var list = new List<ProcessInfo>();
        var processes = Process.GetProcesses();
        int done = 0;

        foreach (var proc in processes)
        {
            string? path = PlatformHelpers.TryGet(() => proc.MainModule?.FileName);
            DateTime? start = PlatformHelpers.TryGet(() => proc.StartTime);

            string? hash = PlatformHelpers.GetSha256(path);

            list.Add(new ProcessInfo(proc.Id, proc.ProcessName, path, hash, null, start));
            progress?.Report(new ScanProgress(++done, processes.Length));
        }

        return list;
    }

    public IEnumerable<UserInfo> ScanUsers(IProgress<ScanProgress>? progress = null)
    {
        var users = CachedUsersInfo.ToList();
        progress?.Report(new ScanProgress(users.Count, users.Count));
        return users;
    }
}
