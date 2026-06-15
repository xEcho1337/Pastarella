using System.Diagnostics;
using Pastarella.Core.Models;

namespace Pastarella.Core.MacOS;

public class ForensicScanner : IForensicScanner
{
    public IEnumerable<ProcessInfo> ScanProcesses()
    {
        var list = new List<ProcessInfo>();

        foreach (var proc in Process.GetProcesses())
        {
            string? path = PlatformHelpers.TryGet(() => proc.MainModule?.FileName);
            Dictionary<string, object> metadata = [];
            string company = string.Empty;
            string product = string.Empty;
            DateTime? start = PlatformHelpers.TryGet(() => proc.StartTime);

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
            list.Add(new ProcessInfo(proc.Id, proc.ProcessName, path, hash, null, start) { Metadata = metadata });
        }

        return list;
    }

    public IEnumerable<UserInfo> ScanUsers()
    {
        var users = new List<UserInfo>();
        try
        {
            string output = PlatformHelpers.RunProcessAndCaptureOutput("dscl", ". -list /Users");
            using var sr = new StringReader(output);

            while (sr.ReadLine() is { } user)
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
            }
        }
        catch
        {
            // ignore
        }

        return users;
    }
}
