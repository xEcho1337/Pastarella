using Pastarella.Core.Models;

namespace Pastarella.Core.Linux;

public class UserScanner : IUserScanner
{
    public static IEnumerable<UserInfo> CachedUsersInfo
    {
        get => field ??= new Unix.UserScanner().Scan();
    }

    public IEnumerable<UserInfo> Scan(IProgress<ScanProgress>? progress = null)
    {
        var users = CachedUsersInfo.ToList();
        progress?.Report(new ScanProgress(users.Count, users.Count));
        return users;
    }
}
