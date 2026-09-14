using System.Security.Cryptography.X509Certificates;
using System.ServiceProcess;
using Microsoft.Win32;
using Pastarella.Core.Models;

namespace Pastarella.Core.Windows;

public class DriverScanner : IDriverScanner
{
    public IEnumerable<DriverInfo> Scan(IProgress<ScanProgress>? progress = null)
    {
        return ServiceController.GetDevices()
            .Select(d =>
            {
                DriverType type = d.ServiceType switch
                {
                    System.ServiceProcess.ServiceType.KernelDriver => DriverType.Kernel,
                    System.ServiceProcess.ServiceType.FileSystemDriver => DriverType.Filesystem,
                    _ => throw new NotImplementedException(),
                };

                var services = Registry.LocalMachine.OpenSubKey(@"SYSTEM\\CurrentControlSet\\Services");
                var key = services?.OpenSubKey(d.ServiceName);

                int nativeStatus = (int)d.Status;

                ExePath? exePath = null;
                if (key?.GetValue("ImagePath")?.ToString() is string imagePath)
                    exePath = new(imagePath);

                return new DriverInfo(
                    d.ServiceName,
                    d.DisplayName,
                    d.ServiceName,
                    type,
                    exePath,
                    null,
                    true
                );
            });
    }
}
