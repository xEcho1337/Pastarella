using Pastarella.Core.Models;

namespace Pastarella.Core.Linux;

public class ProcessScanner(Context ctx) : IProcessScanner
{
    public IEnumerable<ProcessInfo> Scan(IProgress<ScanProgress>? progress = null)
    {
        var list = new List<ProcessInfo>();
        int done = 0;

        string[] systemStat = File.ReadAllLines("/proc/stat");

        string bootTime_raw = systemStat.First(x => x.StartsWith("btime"));
        long bootTime = long.Parse(bootTime_raw["btime".Length..]);

        foreach (uint pid in ctx.PIDs)
        {
            string processDir = Path.Combine("/proc", pid.ToString());
            try
            {
                string stats_raw = File.ReadAllLines(Path.Combine(processDir, "stat"))[0];

                int commandNameStart = stats_raw.IndexOf('(');
                int commandNameEnd = stats_raw.IndexOf(')');

                string[] stats = stats_raw[(commandNameEnd + 2)..].Split(' ');

                ExePath exePath;
                string[]? args = null;

                char state = stats[0][0];
                if (state == 'Z')
                {
                    exePath = new FakeExePath(stats_raw[(commandNameStart + 1)..commandNameEnd]);
                }
                else
                {
                    exePath = new(new FileInfo(Path.Combine(processDir, "exe")).ResolveLinkTarget(false)!.Name, true);

                    string[] cmdline_raw = File.ReadAllLines(Path.Combine(processDir, "cmdline"))[0].Split('\x00', StringSplitOptions.RemoveEmptyEntries);
                    if (cmdline_raw.Length != 0)
                        args = cmdline_raw[1..];
                }

                long startTime = long.Parse(stats[19])! / Unix.Native.LibC.sysconf(Unix.Native.LibC.SysconfName._SC_CLK_TCK);

                list.Add(new ProcessInfo((int)pid)
                {
                    Metadata = [],
                    CommandArgs = args,
                    ExePath = exePath,
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
