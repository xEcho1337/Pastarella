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

[Flags]
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
    public static ServiceType Into(this System.ServiceProcess.ServiceType value) {
        ServiceType type = 0;

        if (value.HasFlag(System.ServiceProcess.ServiceType.KernelDriver))
            type |= ServiceType.KernelDriver;
        if (value.HasFlag(System.ServiceProcess.ServiceType.FileSystemDriver))
            type |= ServiceType.FileSystemDriver;
        if (value.HasFlag(System.ServiceProcess.ServiceType.Adapter))
            type |= ServiceType.Adapter;
        if (value.HasFlag(System.ServiceProcess.ServiceType.RecognizerDriver))
            type |= ServiceType.RecognizerDriver;
        if (value.HasFlag(System.ServiceProcess.ServiceType.Win32OwnProcess))
            type |= ServiceType.Win32OwnProcess;
        if (value.HasFlag(System.ServiceProcess.ServiceType.Win32ShareProcess))
            type |= ServiceType.Win32ShareProcess;
        if (value.HasFlag(System.ServiceProcess.ServiceType.InteractiveProcess))
            type |= ServiceType.InteractiveProcess;

        return type;
    }
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
