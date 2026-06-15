using Pastarella.Core.Models;

namespace Pastarella.Terminal.Outputs.Txt;

public class HostsScanner(OutputBuffer buffer)
{
    private readonly OutputBuffer Buffer = buffer;

    public void WriteHosts(IEnumerable<Host> hosts)
    {
        foreach (var host in hosts)
            Buffer.WriteLine($"{host.Ip}\t{host.Domain}");
    }
}
