using System.Diagnostics;
using System.Management;
using System.Security.Cryptography.X509Certificates;
using Pastarella.Core.Models;
using static Vanara.PInvoke.NetApi32;
using static Vanara.PInvoke.AdvApi32;
using static Vanara.PInvoke.Kernel32;

namespace Pastarella.Core.Windows;

public class ForensicScanner : IForensicScanner
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

    private static Dictionary<int, string> GetCommandLines()
    {
        var map = new Dictionary<int, string>();
        PlatformHelpers.TryDo(() =>
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT ProcessId, CommandLine FROM Win32_Process");

            foreach (var mo in searcher.Get().Cast<ManagementObject>())
            {
                PlatformHelpers.TryDo(() => {
                    int pid = Convert.ToInt32(mo["ProcessId"]);
                    if (mo["CommandLine"] is string cmd && !string.IsNullOrWhiteSpace(cmd))
                        map[pid] = cmd;
                });
            }
        });

        return map;
    }

    public IEnumerable<ProcessInfo> ScanProcesses(IProgress<ScanProgress>? progress = null)
    {
        var result = new List<ProcessInfo>();
        var processes = Process.GetProcesses();
        var cmdlines = GetCommandLines();
        int done = 0;

        foreach (var process in processes)
        {
            Dictionary<string, object> metadata = [];
            string? signer = null;
            string? path = PlatformHelpers.TryGet(() => process.MainModule?.FileName);
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

            string? hash = PlatformHelpers.GetSha256(path);

            cmdlines.TryGetValue(process.Id, out string? cmdline);

            result.Add(new ProcessInfo(process.Id)
            {
                Metadata = metadata,
                CommandArgs = cmdline,
                Path = path,
                Sha256 = hash,
                Signer = signer,
                StartTime = startTime
            });
            progress?.Report(new ScanProgress(++done, processes.Length));
        }

        return result;
    }

    public IEnumerable<UserInfo> ScanUsers(IProgress<ScanProgress>? progress = null)
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
