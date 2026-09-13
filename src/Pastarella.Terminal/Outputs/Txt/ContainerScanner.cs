using Pastarella.Core.Models;

namespace Pastarella.Terminal.Outputs.Txt;

public class ContainerScanner(OutputBuffer buffer)
{
    private readonly OutputBuffer Buffer = buffer;

    public void Write(IEnumerable<ContainerInfo> containers)
    {
        Buffer.WriteLine("[Type(s)] Parent PID | children PIDs");
        foreach (var container in containers)
        {
            Buffer.WriteLine($"[{container.Type}] {container.ParentPID?.ToString() ?? "N/A"} | {string.Join(", ", container.ChildrenPIDs)}");
            TxtWriter.BasicPrintMetadata(Buffer, container.Metadata);
        }
    }
}
