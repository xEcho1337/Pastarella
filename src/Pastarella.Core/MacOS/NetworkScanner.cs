using System.Diagnostics;
using Pastarella.Core.Models;

namespace Pastarella.Core.MacOS;

public class NetworkScanner : INetworkScanner
{
    public IEnumerable<PortInfo> Scan()
    {
        var psi = new ProcessStartInfo()
        {
            FileName = "lsof",
            Arguments = "-nP -i -FpcuPtnT",
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        var process = Process.Start(psi);

        if (process == null)
            return [];

        var ports = new List<PortInfo>();

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
                        AddToPorts(processId, processName, ports, info);

                    info = new RecordInfo();
                    break;

                case 'P':
                    info!.ConnectionType = line[1..];
                    break;

                case 'n':
                    info!.Connection = line[1..];
                    break;

                case 'T':
                    string value = line[1..];

                    if (value.StartsWith("ST="))
                        info!.State = value[3..];

                    break;
            }
        }

        AddToPorts(processId, processName, ports, info);

        return ports;
    }

    private void AddToPorts(uint processId, string processName, List<PortInfo> ports, RecordInfo? info)
    {
        if (info == null) return;

        var portInfo = info.ToPortInfo(processId, processName);

        if (portInfo is not null)
            ports.Add(portInfo);
    }

    private class RecordInfo
    {
        public string ConnectionType { get; set; } = string.Empty;
        public string Connection { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;

        public PortInfo? ToPortInfo(uint processId, string processName)
        {
            if (Connection == "*:*")
                return null;

            ParseConnection(Connection,
                out var local,
                out var remote);

            if (local is null)
                return null;

            return ConnectionType switch
            {
                "TCP" => new TcpPortInfo(
                    processName, processId, State, local, remote
                ),

                "UDP" => new UdpPortInfo(processName, processId, local),

                _ => null
            };
        }

        private static void ParseConnection(
            string value,
            out IpPort? local,
            out IpPort? remote
        )
        {
            remote = null;
            string[] endpoints = value.Split("->", 2);

            ParseEndpoint(endpoints[0], out local);

            if (endpoints.Length == 2)
                ParseEndpoint(endpoints[1], out remote);
        }

        private static void ParseEndpoint(string endpoint, out IpPort? ipPort)
        {
            ipPort = null;
            int idx = endpoint.LastIndexOf(':');

            if (idx < 0) return;

            string[] parts = endpoint.Split(':');
            string ip = parts[^2].Replace("*", "0.0.0.0");
            string portStr = parts[^1];

            if (ushort.TryParse(portStr, out ushort port))
                ipPort = new IpPort(ip, port);
        }
    }
}
