using System.Runtime.InteropServices;

namespace Pastarella.Core.Unix;

internal static class Bindings
{
    public enum SysconfName : int
    {
        _SC_CLK_TCK = 2,
    }

    [DllImport("libc", SetLastError = true)]
    public static extern long sysconf(SysconfName name);
}
