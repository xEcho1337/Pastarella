using System.ServiceProcess;
using Microsoft.Win32;
using Pastarella.Core.Models;

namespace Pastarella.Core.Windows;

public class ServiceScanner : IServiceScanner
{
    public IEnumerable<ServiceInfo> Scan(IProgress<ScanProgress>? progress = null)
    {
        return ServiceController.GetServices().Select(GetServiceInfo);
    }

    private static ServiceInfo GetServiceInfo(ServiceController service)
    {
        var services = Registry.LocalMachine.OpenSubKey(@"SYSTEM\\CurrentControlSet\\Services");
        var key = services?.OpenSubKey(service.ServiceName);

        string[] parts = (key?.GetValue("ImagePath")?.ToString() ?? throw new NotImplementedException()).Split(' ');

        string? hash = PlatformHelpers.GetSha256(parts[0]);
        return new ServiceInfo(
            service.Status.Into(),
            service.ServiceType.Into(),
            service.ServiceName,
            service.DisplayName,
            parts[0],
            parts.Skip(1).ToArray(),
            hash
        );
    }
}
