using System.Runtime.InteropServices;
using Pastarella.Core.Models;

namespace Pastarella.Core.FreeBSD;

public class DriverScanner : IDriverScanner
{
    public IEnumerable<DriverInfo> Scan(IProgress<ScanProgress>? progress = null)
    {
        List<DriverInfo> list = [];

        for (int fileid = Native.LibC.kldnext(0); fileid != 0; fileid = Native.LibC.kldnext(fileid))
        {
            Native.LibC.KldFileStat stat = new();
            if (Native.LibC.kldstat(fileid, ref stat) == -1)
                throw new Exception($"kldstat failed, errno={Marshal.GetLastWin32Error()}");

            list.Add(new(
                stat.name!,
                stat.name!,
                $"file_{stat.id}",
                DriverType.KernelModule,
                new(stat.pathname!, true),
                null /* TODO */,
                true
            ));

            for (int modid = Native.LibC.kldfirstmod(fileid); modid != 0; modid = Native.LibC.modfnext(modid))
            {
                Native.LibC.ModuleStat modStat = new();
                if (Native.LibC.modstat(modid, ref modStat) == -1)
                    throw new Exception($"modstat failed, errno={Marshal.GetLastWin32Error()}");

                list.Add(new(
                    modStat.name!,
                    modStat.name!,
                    $"mod_{modStat.id}",
                    DriverType.KernelModule,
                    new(stat.pathname!, true),
                    null /* TODO */,
                    true
                ));
            }
        }

        return list;
    }
}
