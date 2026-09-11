using System.Runtime.InteropServices;

namespace Pastarella.Core.Unix.Native;

internal static class LibC
{
    [DllImport("libc")]
    public static extern uint getuid();
}
