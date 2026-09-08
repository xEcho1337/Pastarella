using Pastarella.Core.Models;

namespace Pastarella.Core.FreeBSD;

public class ServiceScanner : IServiceScanner
{
    public IEnumerable<ServiceInfo> Scan(IProgress<ScanProgress>? progress = null)
    {
        throw new NotImplementedException();
    }
}
