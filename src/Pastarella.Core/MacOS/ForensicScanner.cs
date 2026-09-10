using System.Diagnostics;
using Pastarella.Core.Models;
using Pastarella.Core.MacOS.Native;

namespace Pastarella.Core.MacOS;

public class ForensicScanner : IForensicScanner
{
    public IEnumerable<ProcessInfo> ScanProcesses(IProgress<ScanProgress>? progress = null)
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

    public IEnumerable<UserInfo> ScanUsers(IProgress<ScanProgress>? progress = null)
    {
        var users = new List<UserInfo>();
        try
        {
            string output = PlatformHelpers.RunProcessAndCaptureOutput("dscl", ". -list /Users");
            string[] names = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            int done = 0;

            foreach (string user in names)
            {
                if (string.IsNullOrWhiteSpace(user)) continue;
                if (user.StartsWith('_')) continue;

                string uniqueId = "";
                string homeDir = "";
                string realName = "";

                string commandOut =
                    PlatformHelpers.RunProcessAndCaptureOutput("dscl", $". -read /Users/{user} RealName UniqueID NFSHomeDirectory");

                string[] lines = commandOut.Split("\n");

                for (int j = 0; j < lines.Length; j++)
                {
                    string l = lines[j];
                    if (l.StartsWith("UniqueID"))
                        uniqueId = l.Replace("UniqueID: ", "").Trim();
                    if (l.StartsWith("NFSHomeDirectory"))
                        homeDir = l.Replace("NFSHomeDirectory: ", "").Trim();

                    if (l.StartsWith("RealName"))
                    {
                        string value = l.Replace("RealName:", "").Trim();

                        if (!string.IsNullOrWhiteSpace(value))
                            realName = value;
                        else if (j + 1 < lines.Length)
                            realName = lines[++j].Trim();
                    }
                }

                users.Add(new UserInfo(user, realName, uniqueId, homeDir, false));
                progress?.Report(new ScanProgress(++done, names.Length));
            }
        }
        catch
        {
            // ignore
        }

        return users;
    }
}
