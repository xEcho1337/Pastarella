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

        string imagePath = key?.GetValue("ImagePath")?.ToString() ?? throw new NotImplementedException();
        string[] parts = PathNormalizer.Unescape(imagePath);

        string? path = null;
        string? hash = null;

        if (parts.Length != 0)
        {
            path = parts[0];
            hash = PlatformHelpers.GetSha256(path);
        }

        return new ServiceInfo(
            service.Status.Into(),
            service.ServiceType.Into(),
            service.ServiceName,
            service.DisplayName,
            path ?? "",
            parts.Skip(1).ToArray(),
            hash
        );
    }
}
