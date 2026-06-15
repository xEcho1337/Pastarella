using Pastarella.Core.Linux.PersistenceScanners;
using Pastarella.Core.Models;

namespace Pastarella.Core.Linux;

public class DriverScanner : IDriverScanner
{
    public IEnumerable<DriverInfo> Scan()
    {
        List<DriverInfo> list = [];

        foreach (string line in File.ReadAllLines("/proc/modules"))
        {
            string[] parts = line.Split(' ', 6);

            string name = parts[0];
            bool loaded = parts[2] != "0";
            string state = parts[4];

            string? filePath = LKMScanner.FindModulePath(name);
            string hash = PlatformHelpers.GetSha256(filePath);

            // TODO: get author and description/display name from module metadata
            list.Add(new(
                name,
                name,
                name,
                DriverType.KernelModule,
                filePath,
                null,
                loaded,
                hash,
                null
            ));
        }

        return list;
    }
}

