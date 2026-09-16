using System.Diagnostics;
using System.Net;
using Pastarella.Core.Models;

namespace Pastarella.Core.MacOS;

public class NetworkScanner : INetworkScanner
{
    public IEnumerable<Socket> Scan(IProgress<ScanProgress>? progress = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "lsof",
            // magic flags to output every connection in multiple lines
            Arguments = "-nP -i -FpcuPtnT",
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        var process = Process.Start(psi);

        if (process == null)
            return [];

        var sockets = new List<Socket>();

        string output = process.StandardOutput.ReadToEnd();
        string[] lines = output.Split("\n");

        uint processId = 0;
        string processName = string.Empty;

        RecordInfo? info = null;

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            char prefix = line[0];

            switch (prefix)
            {
                case 'p':
                    processId = uint.Parse(line[1..]);
                    break;

                case 'c':
                    processName = line[1..];
                    break;

                case 'f':
                    if (info != null)
                        AddToPorts(processId, processName, sockets, info);

                    info = new RecordInfo();
                    break;

                case 'P':
                    info!.ConnectionType = line[1..];
                    break;

                case 'n':
                    info!.Connection = line[1..];
                    break;

                case 't':
                    info!.Version = line[1..];
                    break;

                case 'T':
                    string value = line[1..];

                    if (value.StartsWith("ST="))
                        info!.State = value[3..];

                    break;
            }
        }

        AddToPorts(processId, processName, sockets, info);

        return sockets;
    }

    private void AddToPorts(uint processId, string processName, List<Socket> sockets, RecordInfo? info)
    {
        if (info == null) return;

        var socket = info.ToSocket(processId, processName);

        if (socket is not null)
            sockets.Add(socket);
    }

    private class RecordInfo
    {
        public string ConnectionType { get; set; } = string.Empty;
        public string Connection { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;

        public Socket? ToSocket(uint processId, string processName)
        {
            if (Connection == "*:*")
                return null;

            ParseConnection(Connection,
                Version,
                out var local,
                out var remote);

            return ConnectionType switch
            {
                "TCP" => new(
                    local,
                    remote,
                    new TcpProtocol(State),
                    processId,
                    processName
                ),

                "UDP" => new(
                    local,
                    remote,
                    new UdpProtocol(),
                    processId,
                    processName
                ),

                _ => null
            };
        }

        private static void ParseConnection(
            string value,
            string version,
            out AddressFamily local,
            out AddressFamily? remote
        )
        {
            remote = null;
            string[] endpoints = value.Split("->", 2);

            local = ParseEndpoint(endpoints[0], version)!;

            if (endpoints.Length == 2)
                remote = ParseEndpoint(endpoints[1], version);
        }

        private static AddressFamily? ParseEndpoint(string endpoint, string version)
        {
            int idx = endpoint.LastIndexOf(':');

            if (idx < 0)
                return null;

            // we have to handle "*" differently, according to the IP version
            string allInterfaces = version.Equals("ipv6", StringComparison.CurrentCultureIgnoreCase)
                ? "[::]" : "0.0.0.0";

            // split by the last semi-column to account for both IPv4 and IPv6
            string ipStr = endpoint[..idx].Replace("*", allInterfaces);
            string portStr = endpoint[(idx + 1)..];

            if (!ushort.TryParse(portStr, out ushort port))
                return null;

            return version.ToLower() switch
            {
                "ipv6" => ParseIPv6(ipStr, port),
                "ipv4" => new IPv4Address(ipStr, port),
                _ => null
            };
        }
    }


    private static IPv6Address ParseIPv6(string ipStr, ushort port)
    {
        // lsof may return an IPv4 address for IPv6 connections
        if (!ipStr.Contains('[') && !ipStr.Contains(']'))
            ipStr = IPAddress.Parse(ipStr).MapToIPv6().ToString();

        return new IPv6Address(ipStr, port, null);
    }
}
