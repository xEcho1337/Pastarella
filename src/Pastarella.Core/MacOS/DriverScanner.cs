using Pastarella.Core.MacOS.Drivers;
using Pastarella.Core.Models;

namespace Pastarella.Core.MacOS;

public class DriverScanner : IDriverScanner
{
    public IEnumerable<DriverInfo> Scan()
    {
        List<DriverInfo> list = [];

        var kextScanner = new KextScanner();
        var driverKitScanner = new DriverKitScanner();

        list.AddRange(kextScanner.Scan());
        list.AddRange(driverKitScanner.Scan());

        return list;
    }
}
