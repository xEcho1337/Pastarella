using System.Runtime.InteropServices;

namespace Pastarella.Core.Unix.Native;

internal static class LibC
{
    public enum SysconfName : int
    {
        _SC_CLK_TCK = 2,
    }

    [DllImport("libc", SetLastError = true)]
    public static extern long sysconf(SysconfName name);

    [DllImport("libc")]
    public static extern uint getuid();
}
