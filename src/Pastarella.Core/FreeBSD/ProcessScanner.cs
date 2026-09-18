using Pastarella.Core.Models;
using Bindo.FreeBSD.Structs;

using static Bindo.FreeBSD.LibC.Sysctl;

namespace Pastarella.Core.FreeBSD;

public class ProcessScanner : IProcessScanner
{
    private static KInfoProc[] GetProcesses()
    {
        MIB[] mib = [
            MIB.CTL_KERN,
            MIB.KERN_PROC,
            MIB.KERN_PROC_ALL
        ];

        var processes = Span<KInfoProc>.Empty;
        if (sysctl(mib, ref processes) is Bindo.FreeBSD.Errno e)
            throw new Exception($"sysctl(): failed, errno={e}");

        return processes.ToArray();
    }

    private static string? GetProcessExePath(int pid)
    {
        MIB[] mib = [
            MIB.CTL_KERN,
            MIB.KERN_PROC,
            MIB.KERN_PROC_PATHNAME,
            (MIB)pid
        ];

        var exePath = Span<byte>.Empty;
        if (sysctl(mib, ref exePath) is Bindo.FreeBSD.Errno e)
        {
            if (e == Bindo.FreeBSD.Errno.ESRCH)
                return null;

            throw new Exception($"sysctl(): failed, errno={e}");
        }

        return Bindo.StringHelper.BytesToUTF8(exePath);
    }

    private static string[]? GetProcessArgs(int pid)
    {
        MIB[] mib = [
            MIB.CTL_KERN,
            MIB.KERN_PROC,
            MIB.KERN_PROC_ARGS,
            (MIB)pid
        ];

        var args = Span<byte>.Empty;
        if (sysctl(mib, ref args) is Bindo.FreeBSD.Errno e)
        {
            if (e == Bindo.FreeBSD.Errno.ESRCH)
                return null;

            throw new Exception($"sysctl(): failed, errno={e}");
        }

        return Bindo.StringHelper.BytesToUTF8(args)
            .Split('\0', StringSplitOptions.RemoveEmptyEntries);
    }

    public IEnumerable<ProcessInfo> Scan(IProgress<ScanProgress>? progress = null)
    {
        var list = new List<ProcessInfo>();

        var processes = GetProcesses();
        for (int i = 0; i < processes.Length; i++)
        {
            var p = processes[i];

            string? path = GetProcessExePath(p.ki_pid);
            string[]? args = GetProcessArgs(p.ki_pid);

            list.Add(new(p.ki_pid)
            {
                ExePath = new(string.IsNullOrEmpty(path) ? p.CommandName : path),
                CommandArgs = (args == null || args.Length == 0)
                    ? null
                    : string.Join(' ', args[1..]),

                StartTime = DateTimeOffset.FromUnixTimeSeconds((long)p.ki_start.tv_sec).UtcDateTime,
            });

            i += p.ki_numthreads;
            progress?.Report(new ScanProgress(i + 1));
        }

        return list;
    }
}
