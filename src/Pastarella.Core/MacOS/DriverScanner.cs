using Pastarella.Core.MacOS.Drivers;
using Pastarella.Core.Models;

namespace Pastarella.Core.MacOS;

public class DriverScanner : IDriverScanner
{
    public IEnumerable<DriverInfo> Scan(IProgress<ScanProgress>? progress = null)
    {
        List<DriverInfo> list = [];

        var kextScanner = new KextScanner();
        var driverKitScanner = new DriverKitScanner();

        progress?.Report(new ScanProgress(0, 2, "kext"));
        list.AddRange(kextScanner.Scan(progress));
        progress?.Report(new ScanProgress(1, 2, "driverkit"));
        list.AddRange(driverKitScanner.Scan(progress));
        progress?.Report(new ScanProgress(2, 2));

        return list;
    }
}
