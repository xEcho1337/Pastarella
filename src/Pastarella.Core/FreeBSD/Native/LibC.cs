using System.Runtime.InteropServices;

namespace Pastarella.Core.FreeBSD.Native;

internal static class LibC
{
    const int MAXPATHLEN = 1024;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct KldFileStat()
    {
        public int version = Marshal.SizeOf<KldFileStat>();

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAXPATHLEN)]
        public required string name;

        public int refs;
        public int id;
        public IntPtr address;
        public UIntPtr size;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAXPATHLEN)]
        public required string pathname;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct ModSpecific()
    {
        [FieldOffset(0)]
        public int intval;

        [FieldOffset(0)]
        public uint uintval;

        [FieldOffset(0)]
        public long longval;

        [FieldOffset(0)]
        public ulong ulongval;
    }

    const int MAXMODNAME = MAXPATHLEN;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct ModuleStat()
    {
        public int version = Marshal.SizeOf<ModuleStat>();

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAXMODNAME)]
        public required string name;

        public int refs;
        public int id;
        public ModSpecific data;
    }

    [DllImport("libc", SetLastError = true)]
    public static extern int kldnext(int fileid);

    [DllImport("libc", SetLastError = true)]
    public static extern int kldstat(int fileid, ref KldFileStat stat);

    [DllImport("libc", SetLastError = true)]
    public static extern int kldfirstmod(int fileid);

    [DllImport("libc", SetLastError = true)]
    public static extern int modfnext(int modid);

    [DllImport("libc", SetLastError = true)]
    public static extern int modstat(int modid, ref ModuleStat stat);
}

