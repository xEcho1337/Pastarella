using System.Diagnostics;
using System.Net;
using Pastarella.Core.Models;

namespace Pastarella.Core.Linux;

public class NetworkScanner : INetworkScanner
{
    private static byte[] HexIPv4ToBytes(string hex_ip)
    {
        uint raw = Convert.ToUInt32(hex_ip, 16);
        return BitConverter.GetBytes(raw);
    }

    private static byte[] HexIPv6ToBytes(string hex_ip)
    {
        byte[] arr = Convert.FromHexString(hex_ip);

        for (int i = 0; i < 16; i += 4)
            Array.Reverse(arr, i, 4);

        return arr;
    }

    private static uint? InodeToPid(ulong inode)
    {
        // TODO: optimize (calling this for every row is bad. Have an array of inodes to find).

        foreach (string dir in Directory.EnumerateDirectories("/proc")) {
            string basename = dir.Split('/', 3)[^1];

            // Inside /proc there are also non-processes folders. Skip them
            if (!basename.All(char.IsDigit))
                continue;

            uint pid = uint.Parse(basename);
            try
            {
                foreach (string fd in Directory.EnumerateFiles($"{dir}/fd"))
                {
                    try
                    {
                        var link = new FileInfo(fd).ResolveLinkTarget(false);
                        string socket = link!.Name;
                        if (!socket.StartsWith("socket:["))
                            continue;

                        ulong socket_inode = ulong.Parse(socket["socket:[".Length..^1]);

                        if (socket_inode == inode)
                            return pid;
                    }
                    catch (FileNotFoundException)
                    {
                        // The fd may be closed in the exact time we tried to get it.
                        // Ignore the exception.
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // We may not have enough permissions for opening the directory.
                // Ignore the exception.
            }
        }

        return null;
    }

    private static string PidToProcessName(uint pid)
    {
        return File.ReadAllLines($"/proc/{pid}/cmdline")[0].Split('\x00', ' ')[0];
    }

    private static string ParseTcpState(byte number)
        // linux/include/net/tcp_states.h
        => number switch
        {
            1 => "ESTABLISHED",
            2 => "SYN_SENT",
            3 => "SYN_RECV",
            4 => "FIN_WAIT1",
            5 => "FIN_WAIT2",
            6 => "TIME_WAIT",
            7 => "CLOSE",
            8 => "CLOSE_WAIT",
            9 => "LAST_ACK",
            10 => "LISTEN",
            11 => "CLOSING",
            12 => "NEW_SYN_RECV",
            13 => "BOUND_INACTIVE",

            _ => throw new NotImplementedException($"TCP state: {number}"),
        };

    private static void GetTcpIPv4Connections(ref List<PortInfo> list)
    {
        foreach (string line in File.ReadLines("/proc/net/tcp").Skip(1))
        {
            string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            string[] local = parts[1].Split(':', 2);
            var localAddr = new IPAddress(HexIPv4ToBytes(local[0]));
            ushort localPort = Convert.ToUInt16(local[1], 16);

            string[] remote = parts[2].Split(':', 2);
            var remoteAddr = new IPAddress(HexIPv4ToBytes(remote[0]));
            ushort remotePort = Convert.ToUInt16(remote[1], 16);

            byte connectionState = Convert.ToByte(parts[3], 16);
            ulong inode = Convert.ToUInt64(parts[9]);

            uint? pid = InodeToPid(inode);
            list.Add(new TcpPortInfo(
                (pid == null) ? "N/A" : PidToProcessName(pid.Value),
                pid ?? 0,
                ParseTcpState(connectionState),
                new IpPort(localAddr.ToString(), localPort),
                new IpPort(remoteAddr.ToString(), remotePort)
            ));
        }
    }

    private static void GetUdpIPv4Endpoints(ref List<PortInfo> list)
    {
        foreach (string line in File.ReadLines("/proc/net/udp").Skip(1))
        {
            string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            string[] local = parts[1].Split(':', 2);
            var localAddr = new IPAddress(HexIPv4ToBytes(local[0]));
            ushort localPort = Convert.ToUInt16(local[1], 16);

            ulong inode = Convert.ToUInt64(parts[9]);

            uint? pid = InodeToPid(inode);
            list.Add(new UdpPortInfo(
                (pid == null) ? "N/A" : PidToProcessName(pid.Value),
                pid ?? 0,
                new IpPort(localAddr.ToString(), localPort)
            ));
        }
    }

    private static void GetTcpIPv6Connections(ref List<PortInfo> list)
    {
        foreach (string line in File.ReadLines("/proc/net/tcp6").Skip(1))
        {
            string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            string[] local = parts[1].Split(':', 2);
            var localAddr = new IPAddress(HexIPv6ToBytes(local[0]));
            ushort localPort = Convert.ToUInt16(local[1], 16);

            string[] remote = parts[2].Split(':', 2);
            var remoteAddr = new IPAddress(HexIPv6ToBytes(remote[0]));
            ushort remotePort = Convert.ToUInt16(remote[1], 16);

            byte connectionState = Convert.ToByte(parts[3], 16);
            ulong inode = Convert.ToUInt64(parts[9]);

            uint? pid = InodeToPid(inode);
            list.Add(new TcpPortInfo(
                (pid == null) ? "N/A" : PidToProcessName(pid.Value),
                pid ?? 0,
                ParseTcpState(connectionState),
                new IpPort(localAddr.ToString(), localPort),
                new IpPort(remoteAddr.ToString(), remotePort)
            ));
        }
    }

    private static void GetUdpIPv6Endpoints(ref List<PortInfo> list)
    {
        foreach (string line in File.ReadLines("/proc/net/udp6").Skip(1))
        {
            string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            string[] local = parts[1].Split(':', 2);
            var localAddr = new IPAddress(HexIPv6ToBytes(local[0]));
            ushort localPort = Convert.ToUInt16(local[1], 16);

            ulong inode = Convert.ToUInt64(parts[9]);

            uint? pid = InodeToPid(inode);
            list.Add(new UdpPortInfo(
                (pid == null) ? "N/A" : PidToProcessName(pid.Value),
                pid ?? 0,
                new IpPort(localAddr.ToString(), localPort)
            ));
        }
    }

    public IEnumerable<PortInfo> Scan(IProgress<ScanProgress>? progress = null)
    {
        List<PortInfo> list = [];

        GetTcpIPv4Connections(ref list);
        GetUdpIPv4Endpoints(ref list);

        GetTcpIPv6Connections(ref list);
        GetUdpIPv6Endpoints(ref list);

        return list;
    }
}
