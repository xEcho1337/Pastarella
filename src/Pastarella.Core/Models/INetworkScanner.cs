namespace Pastarella.Core.Models;

public record IpPort(string Ip, ushort Port);

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

public interface INetworkScanner
{
    IEnumerable<PortInfo> Scan(IProgress<ScanProgress>? progress = null);
}
