namespace Pastarella.Core.Models;

public record ScanProgress(int Done, int? Total = null, string? Phase = null);

public interface IForensicScanner
{
    IEnumerable<ProcessInfo> ScanProcesses(IProgress<ScanProgress>? progress = null);
    IEnumerable<UserInfo> ScanUsers(IProgress<ScanProgress>? progress = null);

    IEnumerable<StorageInfo> ScanStorages()
    {
        if (Context.Os == Context.OS.Windows) {
            return Windows.ForensicScanners.Storages.Scan();
        }
        else
        {
            return DriveInfo.GetDrives().Select(d =>
                new StorageInfo(
                    d.DriveType,
                    d.Name,
                    d.IsReady ? (ulong)d.TotalFreeSpace : 0,
                    d.IsReady ? (ulong)d.TotalSize : 0)
            );
        }
    }
}

public interface IServiceScanner
{
    IEnumerable<ServiceInfo> Scan(IProgress<ScanProgress>? progress = null);
}

public interface IDriverScanner
{
    IEnumerable<DriverInfo> Scan(IProgress<ScanProgress>? progress = null);
}

public interface IPersistenceScanner
{
    IEnumerable<PersistenceEntry> Scan(IProgress<ScanProgress>? progress = null);
}

public interface INetworkScanner
{
    IEnumerable<PortInfo> Scan(IProgress<ScanProgress>? progress = null);
}

public interface ICommandHistoryScanner
{
    IEnumerable<CommandHistory> Scan(IProgress<ScanProgress>? progress = null);
}

public record ProcessInfo(int Id)
{
    public Dictionary<string, object> Metadata { get; init; } = [];

    public string? CommandArgs
    {
        get
        {
            if (field != null)
                return field + " ";

            return field;
        }
        init;
    }

    public string? Path { get; init; }
    public string? Sha256 { get; init; }
    public string? Signer { get; init; }
    public DateTime? StartTime { get; init; }
}

public record ServiceInfo(
    ServiceStatus Status, ServiceType ServiceType, string ServiceName, string DisplayName, string ExecPath, string[] Arguments, string? Sha256
);

public record Host(string Ip, string Domain);

public record UserInfo(string Name, string Description, string Uid, string Home, bool Disabled)
{
    public Dictionary<string, object> Metadata { get; init; } = [];
}

public record StorageInfo(DriveType Type, string Name, ulong FreeSpace, ulong TotalSpace);

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

public record RecentFileInfo(string FilePath, DateTime CreationTime, DateTime LastWriteTime);
