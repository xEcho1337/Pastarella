namespace Pastarella.Core.Models;

[Flags]
public enum ContainerType
{ }

public record ContainerInfo(ContainerType Type, uint? ParentPID, uint[] ChildrenPIDs)
{
    public ContainerType Type { get; set; } = Type;

    public Dictionary<string, object> Metadata { get; set; } = [];
}

public interface IContainerScanner
{
    IEnumerable<ContainerInfo> Scan(IProgress<ScanProgress>? progress = null);
}
