using Pastarella.Core.Models;

namespace Pastarella.Core.Linux.ForensicScanners;

public static class Users
{
    public static IEnumerable<UserInfo> CachedUsersInfo
    {
        get => field ??= new Unix.ForensicScanner().ScanUsers();
    }

    public static IEnumerable<UserInfo> Scan(IProgress<ScanProgress>? progress = null)
    {
        var users = CachedUsersInfo.ToList();
        progress?.Report(new ScanProgress(users.Count, users.Count));
        return users;
    }
}
