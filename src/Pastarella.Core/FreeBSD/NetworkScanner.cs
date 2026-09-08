using Pastarella.Core.Models;

namespace Pastarella.Core.FreeBSD;

public class NetworkScanner : INetworkScanner
{
    public IEnumerable<PortInfo> Scan(IProgress<ScanProgress>? progress = null)
    {
        throw new NotImplementedException();
    }
}
