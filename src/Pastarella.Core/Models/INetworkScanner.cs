using System.Text.Json.Serialization;

namespace Pastarella.Core.Models;

[JsonDerivedType(typeof(TcpProtocol))]
[JsonDerivedType(typeof(UdpProtocol))]
public abstract record Protocol(string Name);

public record TcpProtocol(string State) : Protocol("TCP");
public record UdpProtocol() : Protocol("UDP");

[JsonDerivedType(typeof(IPv4Address))]
[JsonDerivedType(typeof(IPv6Address))]
public abstract record AddressFamily(string Type);

public record IPv4Address(
    string Ip,
    ushort Port
) : AddressFamily("IPv4");

public record IPv6Address(
    string Ip,
    ushort Port,
    string? Scope
) : AddressFamily("IPv6");

public record Socket(
    AddressFamily Local,
    AddressFamily? Remote,
    Protocol Protocol,
    uint PID,
    string ProcessName // TODO: remove this. For getting this the PID should be used by programs
);

public interface INetworkScanner
{
    IEnumerable<Socket> Scan(IProgress<ScanProgress>? progress = null);
}
