namespace Pastarella.Core.Models;

[Flags]
public enum ContainerType
{
    // Linux-only
    Cgroup = 0,
    Ipc = 1 << 0,
    Mount = 1 << 1,
    Network = 1 << 2,
    Pid = 1 << 3,
    ChildrenPid = 1 << 4,
    Time = 1 << 5,
    ChildrenTime = 1 << 6,
    User = 1 << 7,
    Uts = 1 << 8,
}

public record ContainerInfo(ContainerType Type, uint? ParentPID, uint[] ChildrenPIDs)
{
    public ContainerType Type { get; set; } = Type;

    public Dictionary<string, object> Metadata { get; set; } = [];
}

public interface IContainerScanner
{
    IEnumerable<ContainerInfo> Scan(IProgress<ScanProgress>? progress = null);
}
