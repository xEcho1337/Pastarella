using System.Diagnostics;

namespace Pastarella.Core.Models;

public enum ServiceStatus
{
    Stopped,
    StartPending,
    StopPending,
    Running,
    ContinuePending,
    PausePending,
    Paused,
}

// For Windows
public static class System_ServiceProcess_ServiceControllerStatusExtensions
{
    public static ServiceStatus Into(this System.ServiceProcess.ServiceControllerStatus value) => value switch
    {
        System.ServiceProcess.ServiceControllerStatus.Stopped => ServiceStatus.Stopped,
        System.ServiceProcess.ServiceControllerStatus.StartPending => ServiceStatus.StartPending,
        System.ServiceProcess.ServiceControllerStatus.StopPending => ServiceStatus.StopPending,
        System.ServiceProcess.ServiceControllerStatus.Running => ServiceStatus.Running,
        System.ServiceProcess.ServiceControllerStatus.ContinuePending => ServiceStatus.ContinuePending,
        System.ServiceProcess.ServiceControllerStatus.PausePending => ServiceStatus.PausePending,
        System.ServiceProcess.ServiceControllerStatus.Paused => ServiceStatus.Paused,
        _ => throw new UnreachableException(),
    };
}

public enum ServiceType
{
    // Windows-only
    KernelDriver,
    FileSystemDriver,
    Adapter,
    RecognizerDriver,
    Win32OwnProcess,
    Win32ShareProcess,
    InteractiveProcess,

    // MacOS-only
    MacOSService,
}

// For Windows
public static class System_ServiceProcess_ServiceTypeExtensions
{
    public static ServiceType Into(this System.ServiceProcess.ServiceType value) => value switch
    {
        System.ServiceProcess.ServiceType.KernelDriver => ServiceType.KernelDriver,
        System.ServiceProcess.ServiceType.FileSystemDriver => ServiceType.FileSystemDriver,
        System.ServiceProcess.ServiceType.Adapter => ServiceType.Adapter,
        System.ServiceProcess.ServiceType.RecognizerDriver => ServiceType.RecognizerDriver,
        System.ServiceProcess.ServiceType.Win32OwnProcess => ServiceType.Win32OwnProcess,
        System.ServiceProcess.ServiceType.Win32ShareProcess => ServiceType.Win32ShareProcess,
        System.ServiceProcess.ServiceType.InteractiveProcess => ServiceType.InteractiveProcess,
        _ => throw new UnreachableException(),
    };
}

public record ServiceInfo(
    ServiceStatus Status,
    ServiceType ServiceType,
    string ServiceName,
    string DisplayName,
    string ExecPath,
    string[] Arguments,
    string? Sha256
);

public interface IServiceScanner
{
    IEnumerable<ServiceInfo> Scan(IProgress<ScanProgress>? progress = null);
}
