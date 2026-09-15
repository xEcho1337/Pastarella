using Pastarella.Core.Models;

namespace Pastarella.Terminal.Outputs.Txt;

public class NetworkService(OutputBuffer buffer)
{
    private readonly OutputBuffer Buffer = buffer;

    private void PrintAddress(AddressFamily address)
        => Buffer.Write(address switch
        {
            IPv4Address addr => $"{addr.Ip}:{addr.Port}",
            IPv6Address addr => $"[{addr.Ip}{(addr.Scope == null ? "" : $"%{addr.Scope}")}]:{addr.Port}",
            _ => throw new NotImplementedException(),
        });

    public void WriteSockets(IEnumerable<Socket> sockets)
    {

        Buffer.WriteLine("Protocol [PID] Name (Local -> Remote) - State");

        foreach (var socket in sockets)
        {
            Buffer.Write($"{socket.Protocol.Name} [{socket.PID}] {socket.ProcessName} ");

            PrintAddress(socket.Local);
            if (socket.Remote is { } remote)
            {
                Buffer.Write($" -> ");
                PrintAddress(remote);
            }

            switch (socket.Protocol)
            {
                case TcpProtocol tcp:
                    Buffer.WriteLine($" - {tcp.State}");
                    break;
                default:
                    Buffer.Write('\n');
                    break;
            }
        }
    }
}
