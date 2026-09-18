using System.Runtime.InteropServices;
using Pastarella.Core.Models;
using static Bindo.FreeBSD.LibC.Kld;

namespace Pastarella.Core.FreeBSD;

public class DriverScanner : IDriverScanner
{
    public IEnumerable<DriverInfo> Scan(IProgress<ScanProgress>? progress = null)
    {
        List<DriverInfo> list = [];

        for (int fileid = kldnext(0); fileid != 0; fileid = kldnext(fileid))
        {
            var stat = KldFileStat.Empty();
            if (kldstat(fileid, ref stat) == -1)
                throw new Exception($"kldstat failed, errno={Marshal.GetLastWin32Error()}");

            list.Add(new(
                stat.Name,
                stat.Name,
                $"file_{stat.id}",
                DriverType.KernelModule,
                new(stat.PathName, true),
                null /* TODO */,
                true
            ));

            for (int modid = kldfirstmod(fileid); modid != 0; modid = modfnext(modid))
            {
                var modStat = ModuleStat.Empty();
                if (modstat(modid, ref modStat) == -1)
                    throw new Exception($"modstat failed, errno={Marshal.GetLastWin32Error()}");

                list.Add(new(
                    modStat.Name,
                    modStat.Name,
                    $"mod_{modStat.id}",
                    DriverType.KernelModule,
                    new(stat.PathName, true),
                    null /* TODO */,
                    true
                ));
            }
        }

        return list;
    }
}
