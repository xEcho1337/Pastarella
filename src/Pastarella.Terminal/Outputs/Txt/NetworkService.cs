using Pastarella.Core.Models;

namespace Pastarella.Terminal.Outputs.Txt;

public class NetworkService(OutputBuffer buffer)
{
    private readonly OutputBuffer Buffer = buffer;

    public void WritePorts(IEnumerable<PortInfo> ports) {

        Buffer.WriteLine("Protocol [PID] Name (Local -> Remote) - State");

        foreach (var port in ports)
        {
            Buffer.Write($"{port.Protocol} [{port.ProcessId}] {port.ProcessName} {port.Local.Ip}:{port.Local.Port}");

            switch (port)
            {
                case TcpPortInfo tcp:
                    string buffer = "";
                    if (tcp.Remote is not null)
                        buffer += $" -> {tcp.Remote.Ip}:{tcp.Remote.Port}";
                    buffer += $" - {tcp.State}";
                    Buffer.WriteLine(buffer);
                    break;
                default:
                    Buffer.Write('\n');
                    break;
            }
        }
    }
}

