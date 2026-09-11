using Pastarella.Core.Models;

namespace Pastarella.Core.Linux.ForensicScanners;

public static class Processes
{
    public static IEnumerable<ProcessInfo> Scan(IProgress<ScanProgress>? progress = null)
    {
        var list = new List<ProcessInfo>();
        int done = 0;

        string[] systemStat = File.ReadAllLines("/proc/stat");

        string bootTime_raw = systemStat.First(x => x.StartsWith("btime"));
        long bootTime = long.Parse(bootTime_raw["btime".Length..]);

        foreach (string dir in Directory.EnumerateDirectories("/proc"))
        {
            string basename = dir.Split('/', 3)[^1];

            // Inside /proc there are also non-processes folders. Skip them
            if (!basename.All(char.IsDigit))
                continue;

            int pid = int.Parse(basename);
            try
            {
                string stats_raw = File.ReadAllLines(Path.Combine(dir, "stat"))[0];

                int commandNameStart = stats_raw.IndexOf('(');
                int commandNameEnd = stats_raw.IndexOf(')');

                string[] stats = stats_raw[(commandNameEnd + 2)..].Split(' ');

                string path;
                string? hash = null;
                string? args = null;

                char state = stats[0][0];
                if (state == 'Z')
                {
                    path = stats_raw[(commandNameStart + 1)..commandNameEnd];
                }
                else
                {
                    path = new FileInfo(Path.Combine(dir, "exe")).ResolveLinkTarget(false)!.Name;
                    hash = PlatformHelpers.GetSha256(path);

                    string[] cmdline_raw = File.ReadAllLines(Path.Combine(dir, "cmdline"))[0].Split('\x00', StringSplitOptions.RemoveEmptyEntries)[..^1];
                    if (cmdline_raw.Length != 0)
                        args = string.Join(' ', cmdline_raw[1..]);
                }

                long startTime = long.Parse(stats[19])! / Unix.Native.LibC.sysconf(Unix.Native.LibC.SysconfName._SC_CLK_TCK);

                list.Add(new ProcessInfo(pid)
                {
                    Metadata = [],
                    CommandArgs = args,
                    Path = path,
                    Sha256 = hash,
                    Signer = null,
                    StartTime = DateTimeOffset.FromUnixTimeSeconds(bootTime + startTime).UtcDateTime,
                });
                progress?.Report(new ScanProgress(++done));
            }
            catch (UnauthorizedAccessException)
            {
                // We may not have enough permissions for opening the directory.
                // Ignore the exception.
            }
        }

        return list;
    }
}

