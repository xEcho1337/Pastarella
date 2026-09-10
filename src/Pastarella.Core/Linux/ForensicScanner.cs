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
        var result = new List<ProcessInfo>();
        var processes = Process.GetProcesses();
        int done = 0;

        foreach (var proc in processes)
        {
            string? path = PlatformHelpers.TryGet(() => proc.MainModule?.FileName);
            DateTime? start = PlatformHelpers.TryGet(() => proc.StartTime);

            string? hash = PlatformHelpers.GetSha256(path);

            result.Add(new ProcessInfo(proc.Id)
            {
                Metadata = [],
                CommandArgs = null, // TODO
                Path = path,
                Sha256 = hash,
                Signer = null,
                StartTime = start
            });
            progress?.Report(new ScanProgress(++done, processes.Length));
        }

        return result;
    }

    public IEnumerable<UserInfo> ScanUsers(IProgress<ScanProgress>? progress = null)
    {
        var users = CachedUsersInfo.ToList();
        progress?.Report(new ScanProgress(users.Count, users.Count));
        return users;
    }
}
