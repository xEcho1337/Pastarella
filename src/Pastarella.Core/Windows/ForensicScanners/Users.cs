using Pastarella.Core.Models;

using static Vanara.PInvoke.NetApi32;
using static Vanara.PInvoke.AdvApi32;
using static Vanara.PInvoke.Kernel32;

namespace Pastarella.Core.Windows.ForensicScanners;

public static class Users
{
    public static List<(USER_INFO_1, string /* SID */)> CachedUserInfo => field ??= GetUserInfo();

    private static List<(USER_INFO_1, string)> GetUserInfo()
    {

        List<(USER_INFO_1, string)> list = [];

        foreach (var user in NetUserEnum<USER_INFO_1>(null))
        {
            if (!LookupAccountName(null, user.usri1_name, out var sid, out string _, out var _))
                throw new Exception(GetLastError().ToString());

            list.Add((user, sid.ToString()));
        }

        return list;
    }

    public static IEnumerable<UserInfo> Scan(IProgress<ScanProgress>? progress = null)
    {
        int done = 0;

        foreach (var u in CachedUserInfo)
        {
            (var info, string sid) = u;
            yield return new UserInfo(
                info.usri1_name,
                info.usri1_comment ?? "",
                sid,
                info.usri1_home_dir ?? "",
                (info.usri1_flags & UserAcctCtrlFlags.UF_ACCOUNTDISABLE) != 0
            )
            {
                Metadata =
                {
                    ["lockout"] = (info.usri1_flags & UserAcctCtrlFlags.UF_LOCKOUT) != 0,
                }
            };

            progress?.Report(new ScanProgress(++done, CachedUserInfo.Count));
        }
    }
}
