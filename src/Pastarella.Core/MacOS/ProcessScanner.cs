using System.Diagnostics;
using Pastarella.Core.Models;
using Pastarella.Core.MacOS.Native;

namespace Pastarella.Core.MacOS;

public class ProcessScanner : IProcessScanner
{
    public IEnumerable<ProcessInfo> Scan(IProgress<ScanProgress>? progress = null)
    {
        var list = new List<ProcessInfo>();
        var processes = Process.GetProcesses();
        int done = 0;

        foreach (var proc in processes)
        {
            Dictionary<string, object> metadata = [];

            DateTime? start = PlatformHelpers.TryGet(() => proc.StartTime);
            string? path = PlatformHelpers.TryGet(() => MacOsProcess.GetExePath(proc.Id));
            string? cmdline = PlatformHelpers.TryGet(() => MacOsProcess.GetCommandLine(proc.Id));

            if (cmdline != null && path != null)
                cmdline = cmdline.Replace(path, "");

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

                // TODO: macOS code signing
            }

            string? hash = PlatformHelpers.GetSha256(path);
            list.Add(new ProcessInfo(proc.Id)
            {
                Metadata = metadata,
                CommandArgs = cmdline,
                Path = path,
                Sha256 = hash,
                StartTime = start

            });
            progress?.Report(new ScanProgress(++done, processes.Length));
        }

        return list;
    }
}
