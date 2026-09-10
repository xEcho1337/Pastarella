using System.Management;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using Pastarella.Core.Models;

namespace Pastarella.Core.Windows.ForensicScanners;

public static class Processes
{
    private static Dictionary<int, string> GetCommandLines()
    {
        var map = new Dictionary<int, string>();
        PlatformHelpers.TryDo(() =>
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT ProcessId, CommandLine FROM Win32_Process");

            foreach (var mo in searcher.Get().Cast<ManagementObject>())
            {
                PlatformHelpers.TryDo(() =>
                {
                    int pid = Convert.ToInt32(mo["ProcessId"]);
                    if (mo["CommandLine"] is string cmd && !string.IsNullOrWhiteSpace(cmd))
                        map[pid] = cmd;
                });
            }
        });

        return map;
    }

    public static IEnumerable<ProcessInfo> Scan(IProgress<ScanProgress>? progress = null)
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
}
