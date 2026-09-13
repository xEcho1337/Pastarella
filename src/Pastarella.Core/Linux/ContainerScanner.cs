using Pastarella.Core.Models;

namespace Pastarella.Core.Linux;

public class ContainerScanner(Context ctx) : IContainerScanner
{
    private static ContainerType NsNameToType(string ns) => ns switch
    {
        "cgroup" => ContainerType.Cgroup,
        "ipc" => ContainerType.Ipc,
        "mnt" => ContainerType.Mount,
        "net" => ContainerType.Network,
        "pid" => ContainerType.Pid,
        "pid_for_children" => ContainerType.ChildrenPid,
        "time" => ContainerType.Time,
        "time_for_children" => ContainerType.ChildrenTime,
        "user" => ContainerType.User,
        "uts" => ContainerType.Uts,
        _ => throw new NotImplementedException(),
    };

    // No try-catch since we are already handling exceptions on `GetNSes`.
    private static void GetUserNsMetadata(string processDir, ref Dictionary<string, object> metadata)
    {
        string[] uidMap = File.ReadAllLines(Path.Combine(processDir, "uid_map"))[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        metadata["NsUidStart"] = uidMap[0];
        metadata["NsUidMapsTo"] = uidMap[1];
        metadata["NsUidSize"] = uidMap[2];

        string[] gidMap = File.ReadAllLines(Path.Combine(processDir, "gid_map"))[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        metadata["NsGidStart"] = gidMap[0];
        metadata["NsGidMapsTo"] = gidMap[1];
        metadata["NsGidSize"] = gidMap[2];
    }

    // No try-catch since we are already handling exceptions on `GetNSes`.
    private static void GetCgroupMetadata(string processDir, ref Dictionary<string, object> metadata)
    {
        string[] info = File.ReadAllLines(Path.Combine(processDir, "cgroup"));

        var parseVersion1 = (ref Dictionary<string, object> metadata, string[] parts) =>
        {
            metadata["CgroupV1Id"] = ulong.Parse(parts[0]);
            metadata["CgroupV1Controllers"] = parts[1].Split(',');
            metadata["CgroupV1Path"] = parts[2];
        };

        var parseVersion2 = (ref Dictionary<string, object> metadata, string[] parts) =>
        {
            metadata["CgroupV2Id"] = ulong.Parse(parts[0]);
            metadata["CgroupV2Path"] = parts[2];
        };

        if (info.Length > 1)
        {
            foreach (string entry in info)
                parseVersion1(ref metadata, entry.Split(':'));
        }
        else
        {
            string[] split = info[0].Split(':');
            if (split[1].Length == 0)
                parseVersion2(ref metadata, split);
            else
                parseVersion1(ref metadata, split);
        }
    }

    private static void CompactNSes(ref List<ContainerInfo> list)
    {
        if (list.Count < 2)
            return;

        for (int i = 0; i < list.Count - 1; i++)
        {
            for (int j = list.Count - 1; j > i; j--)
            {
                var first = list[i];
                var second = list[j];

                if (!first.ChildrenPIDs.SequenceEqual(second.ChildrenPIDs))
                    continue;

                first.Type |= second.Type;
                first.ChildrenPIDs.Concat(second.ChildrenPIDs).Distinct();
                first.Metadata.Concat(second.Metadata);

                list.RemoveAt(j);
            }
        }
    }

    private static ulong? TryGetNSInode(string nsPath)
    {
        try
        {
            var link = new FileInfo(nsPath).ResolveLinkTarget(false);
            return ulong.Parse(link!.Name[(link!.Name.IndexOf('[') + 1)..^1]);
        }
        catch (FileNotFoundException)
        {
            // We cannot read /proc/PID/ns of zombie processes.
            return null;
        }
    }

    private uint[] GetPIDsOfNs(string nsName, ulong nsInode)
    {
        var pids = new List<uint>();

        foreach (uint pid in ctx.PIDs)
        {
            string nsPath = Path.Combine("/proc", pid.ToString(), "ns", nsName);

            try
            {
                if (!File.Exists(nsPath))
                    continue;

                if (TryGetNSInode(nsPath) is not ulong inode)
                    continue;

                if (nsInode == inode)
                    pids.Add(pid);
            }
            catch (UnauthorizedAccessException)
            {
                // We may not have enough permissions for opening the directory.
                // Ignore the exception.
            }
            catch (DirectoryNotFoundException)
            {
                // The process may exit while we are enumerating it.
                // Ignore the exception.
            }
        }

        return [.. pids];
    }

    private void GetNSes(ref List<ContainerInfo> list)
    {
        foreach (uint pid in ctx.PIDs)
        {
            string processDir = Path.Combine("/proc", pid.ToString());
            try
            {
                foreach (string ns in Directory.EnumerateFiles(Path.Combine(processDir, "ns")))
                {
                    string nsName = ns.Split('/')[^1];

                    if (TryGetNSInode(ns) is not ulong nsInode)
                        continue;

                    Dictionary<string, object> metadata = [];
                    var containerType = NsNameToType(nsName);

                    switch (containerType)
                    {
                        case ContainerType.Cgroup:
                            GetCgroupMetadata(processDir, ref metadata);
                            break;
                        case ContainerType.User:
                            GetUserNsMetadata(processDir, ref metadata);
                            break;
                        default:
                            break;
                    }

                    list.Add(
                        new(
                            containerType,
                            null,
                            GetPIDsOfNs(nsName, nsInode)
                        )
                        {
                            Metadata = metadata
                        }
                    );
                }
            }
            catch (UnauthorizedAccessException)
            {
                // We may not have enough permissions for opening the directory.
                // Ignore the exception.
            }
            catch (DirectoryNotFoundException)
            {
                // The process may exit while we are enumerating it.
                // Ignore the exception.
            }
        }
    }

    public IEnumerable<ContainerInfo> Scan(IProgress<ScanProgress>? progress = null)
    {
        var list = new List<ContainerInfo>();

        GetNSes(ref list);
        CompactNSes(ref list);

        return list;
    }
}
