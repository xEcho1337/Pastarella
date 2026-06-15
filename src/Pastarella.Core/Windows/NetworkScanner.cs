using System.Diagnostics;
using System.Net;
using Pastarella.Core.Models;
using static Vanara.PInvoke.IpHlpApi;
using static Vanara.PInvoke.Ws2_32;

namespace Pastarella.Core.Windows;

public class NetworkScanner : INetworkScanner
{
    private static string StringifyTcpState(MIB_TCP_STATE state)
        => state switch
        {
            MIB_TCP_STATE.MIB_TCP_STATE_CLOSED => "CLOSED",
            MIB_TCP_STATE.MIB_TCP_STATE_LISTEN => "LISTEN",
            MIB_TCP_STATE.MIB_TCP_STATE_SYN_SENT => "SYN_SENT",
            MIB_TCP_STATE.MIB_TCP_STATE_SYN_RCVD => "SYN_RCVD",
            MIB_TCP_STATE.MIB_TCP_STATE_ESTAB => "ESTABILISH",
            MIB_TCP_STATE.MIB_TCP_STATE_FIN_WAIT1 => "FIN_WAIT1",
            MIB_TCP_STATE.MIB_TCP_STATE_FIN_WAIT2 => "FIN_WAIT2",
            MIB_TCP_STATE.MIB_TCP_STATE_CLOSE_WAIT => "CLOSE_WAIT",
            MIB_TCP_STATE.MIB_TCP_STATE_CLOSING => "CLOSING",
            MIB_TCP_STATE.MIB_TCP_STATE_LAST_ACK => "LAST_ACK",
            MIB_TCP_STATE.MIB_TCP_STATE_TIME_WAIT => "TIME_WAIT",
            MIB_TCP_STATE.MIB_TCP_STATE_DELETE_TCB => "DELETE_TCB",

            _ => throw new NotImplementedException($"TCP state: {state}"),
        };

    private static void GetTcpConnections(ref List<PortInfo> list)
    {
        foreach (var entry in GetExtendedTcpTable<MIB_TCPTABLE_OWNER_PID>(TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, ADDRESS_FAMILY.AF_INET))
        {
            list.Add(
                new TcpPortInfo(
                    Process.GetProcessById((int)entry.dwOwningPid).ProcessName,
                    entry.dwOwningPid,
                    StringifyTcpState(entry.dwState),
                    new IpPort(
                        new IPAddress(entry.dwLocalAddr.S_un_b).ToString(),
                        (ushort)entry.dwLocalPort),
                    new IpPort(
                        new IPAddress(entry.dwRemoteAddr.S_un_b).ToString(),
                        (ushort)entry.dwRemotePort)
                )
            );
        }

        foreach (var entry in GetExtendedTcpTable<MIB_TCP6TABLE_OWNER_PID>(TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, ADDRESS_FAMILY.AF_INET6))
        {
            list.Add(
                new TcpPortInfo(
                    Process.GetProcessById((int)entry.dwOwningPid).ProcessName,
                    entry.dwOwningPid,
                    StringifyTcpState(entry.dwState),
                    new IpPort(
                        $"[{new IPAddress(entry.ucLocalAddr.bytes, entry.dwLocalScopeId)}]",
                        (ushort)entry.dwLocalPort),
                    new IpPort(
                        $"[{new IPAddress(entry.ucRemoteAddr.bytes, entry.dwRemoteScopeId)}]",
                        (ushort)entry.dwRemotePort)
                )
            );
        }
    }

    private static void GetUdpEndpoints(ref List<PortInfo> list)
    {
        foreach (var entry in GetExtendedUdpTable<MIB_UDPTABLE_OWNER_PID>(UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID, ADDRESS_FAMILY.AF_INET))
        {
            string ip = new IPAddress(entry.dwLocalAddr.S_un_b).ToString();
            ushort port = (ushort)entry.dwLocalPort;
            list.Add(
                new UdpPortInfo(
                    Process.GetProcessById((int)entry.dwOwningPid).ProcessName,
                    entry.dwOwningPid,
                    new IpPort(ip, port)
                )
            );
        }

        foreach (var entry in GetExtendedUdpTable<MIB_UDP6TABLE_OWNER_PID>(UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID, ADDRESS_FAMILY.AF_INET6))
        {
            string ip = new IPAddress(entry.ucLocalAddr.bytes, entry.dwLocalScopeId).ToString();
            ushort port = (ushort) entry.dwLocalPort;
            list.Add(
                new UdpPortInfo(
                    Process.GetProcessById((int)entry.dwOwningPid).ProcessName,
                    entry.dwOwningPid,
                    new IpPort($"[{ip}]", port)
                )
            );
        }
    }

    public IEnumerable<PortInfo> Scan()
    {
        List<PortInfo> list = [];

        GetTcpConnections(ref list);
        GetUdpEndpoints(ref list);

        return list;
    }
}
