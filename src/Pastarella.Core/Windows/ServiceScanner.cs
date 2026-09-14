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
        var exePath = ExePath.FromCmdline(imagePath, out string[] args);

        return new ServiceInfo(
            service.Status.Into(),
            service.ServiceType.Into(),
            service.ServiceName,
            service.DisplayName,
            exePath,
            args
        );
    }
}
