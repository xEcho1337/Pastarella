using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using Pastarella.Core.Models;
using static Vanara.PInvoke.NetApi32;
using static Vanara.PInvoke.AdvApi32;
using static Vanara.PInvoke.Kernel32;

namespace Pastarella.Core.Windows;

public class ForensicScanner : IForensicScanner
{
    public static List<(USER_INFO_1, string /* SID */)> CachedUserInfo
    {
        get => field ??= GetUserInfo();
    }

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

    public IEnumerable<ProcessInfo> ScanProcesses()
    {
        foreach (var process in Process.GetProcesses())
        {
            string? path = PlatformHelpers.TryGet(() => process.MainModule?.FileName);
            Dictionary<string, object> metadata = [];
            string? signer = null;
            DateTime? startTime = PlatformHelpers.TryGet(() => process.StartTime);

            if (!string.IsNullOrWhiteSpace(path))
            {
                PlatformHelpers.TryExecNotNull(
                    () => FileVersionInfo.GetVersionInfo(path),
                    versionInfo =>
                    {
                        if (versionInfo.CompanyName != null)
                            metadata["Company"] = versionInfo.CompanyName;

                        if (versionInfo.ProductName != null)
                            metadata["Product"] = versionInfo.ProductName;
                    });

                PlatformHelpers.TryDo(() => signer = X509Certificate.CreateFromSignedFile(path).Subject);
            }

            yield return new ProcessInfo(
                process.Id,
                process.ProcessName,
                path,
                PlatformHelpers.GetSha256(path),
                signer,
                startTime
            )
            {
                Metadata = metadata,
            };
        }
    }

    public IEnumerable<UserInfo> ScanUsers()
    {
        return CachedUserInfo.Select(u =>
        {
            var (info, sid) = u;
            return new UserInfo(
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
                },
            };
        });
    }
}
