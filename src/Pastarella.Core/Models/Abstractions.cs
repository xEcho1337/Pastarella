namespace Pastarella.Core.Models;

public interface IForensicScanner
{
    IEnumerable<ProcessInfo> ScanProcesses();
    IEnumerable<UserInfo> ScanUsers();

    IEnumerable<StorageInfo> ScanStorages()
    {
        return DriveInfo.GetDrives().Select(d =>
            new StorageInfo(
                d.DriveType,
                d.Name,
                d.IsReady ? d.TotalFreeSpace : 0,
                d.IsReady ? d.TotalSize : 0)
        );
    }
}

public interface IServiceScanner
{
    IEnumerable<ServiceInfo> Scan();
}

public interface IDriverScanner
{
    IEnumerable<DriverInfo> Scan();
}

public interface IPersistenceScanner
{
    IEnumerable<PersistenceEntry> Scan();
}

public interface INetworkScanner
{
    IEnumerable<PortInfo> Scan();
}

public interface ICommandHistoryScanner
{
    IEnumerable<CommandHistory> Scan();
}

public record ProcessInfo(
    int Id,
    string Name,
    string? Path,
    string? Sha256,
    string? Signer,
    DateTime? StartTime
)
{
    public Dictionary<string, object> Metadata { get; init; } = [];
}

public record ServiceInfo(
    ServiceStatus Status, ServiceType ServiceType, string ServiceName, string DisplayName, string ExecPath, string[] Arguments, string? Sha256
);

public record Host(string Ip, string Domain);

public record UserInfo(string Name, string Description, string Uid, string Home, bool Disabled)
{
    public Dictionary<string, object> Metadata { get; init; } = [];
}

public record StorageInfo(DriveType Type, string Name, long FreeSpace, long TotalSpace);

public record DriverInfo(
    string Name,
    string DisplayName,
    string Identifier,
    DriverType Type,
    string? ExecutablePath,
    string? Version,
    bool Loaded,
    string? Sha256,
    string? Signer
);

public abstract record PortInfo(
    string Protocol,
    string ProcessName,
    uint ProcessId,
    IpPort Local
);

public record TcpPortInfo(
    string ProcessName,
    uint ProcessId,
    string State,
    IpPort Local,
    IpPort? Remote
) : PortInfo("TCP", ProcessName, ProcessId, Local);

public record UdpPortInfo(
    string ProcessName,
    uint ProcessId,
    IpPort Local
) : PortInfo("UDP", ProcessName, ProcessId, Local);

public record IpPort(string Ip, ushort Port);

public record CommandHistory(string Shell, IEnumerable<string> Commands);
