using System.ServiceProcess;
using Microsoft.Win32;
using Pastarella.Core.Models;
using ServiceType = Pastarella.Core.Models.ServiceType;

namespace Pastarella.Core.Windows;

public class ServiceScanner : IServiceScanner
{
    public IEnumerable<ServiceInfo> Scan()
    {
        return ServiceController.GetServices().Select(GetServiceInfo);
    }

    private static ServiceInfo GetServiceInfo(ServiceController service)
    {
        var services = Registry.LocalMachine.OpenSubKey(@"SYSTEM\\CurrentControlSet\\Services");
        var key = services?.OpenSubKey(service.ServiceName);

        int nativeStatus = (int)service.Status;
        int nativeType = (int)service.ServiceType;

        string[] parts = (key?.GetValue("ImagePath")?.ToString() ?? throw new NotImplementedException()).Split(' ');

        string? hash = PlatformHelpers.GetSha256(parts[0]);
        return new ServiceInfo(
            (ServiceStatus)nativeStatus,
            (ServiceType)nativeType,
            service.ServiceName,
            service.DisplayName,
            parts[0],
            parts.Skip(1).ToArray(),
            hash
        );
    }
}
