using System.Runtime.InteropServices;
using Pastarella.Core.Models;

namespace Pastarella.Core.FreeBSD;

public class DriverScanner : IDriverScanner
{
    internal static class Bindings
    {
        const int MAXPATHLEN = 1024;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct KldFileStat()
        {
            public int version = Marshal.SizeOf<KldFileStat>();

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAXPATHLEN)]
            public string name;

            public int refs;
            public int id;
            public IntPtr address;
            public UIntPtr size;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAXPATHLEN)]
            public string pathname;
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
            public string name;

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

    public IEnumerable<DriverInfo> Scan()
    {
        List<DriverInfo> list = [];

        for (int fileid = Bindings.kldnext(0); fileid != 0; fileid = Bindings.kldnext(fileid))
        {
            Bindings.KldFileStat stat = new();
            if (Bindings.kldstat(fileid, ref stat) == -1)
                throw new Exception($"kldstat failed, errno={Marshal.GetLastWin32Error()}");

            string? modHash = PlatformHelpers.GetSha256(stat.pathname);
            list.Add(new(
                stat.name,
                stat.name,
                $"file_{stat.id}",
                DriverType.KernelModule,
                stat.pathname,
                null /* TODO */,
                true,
                modHash,
                null
            ));

            for (int modid = Bindings.kldfirstmod(fileid); modid != 0; modid = Bindings.modfnext(modid))
            {
                Bindings.ModuleStat modStat = new();
                if (Bindings.modstat(modid, ref modStat) == -1)
                    throw new Exception($"modstat failed, errno={Marshal.GetLastWin32Error()}");

                list.Add(new(
                    modStat.name,
                    modStat.name,
                    $"mod_{modStat.id}",
                    DriverType.KernelModule,
                    stat.pathname,
                    null /* TODO */,
                    true,
                    modHash,
                    null
                ));
            }
        }

        return list;
    }
}
