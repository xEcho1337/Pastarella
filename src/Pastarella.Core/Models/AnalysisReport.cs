using Pastarella.Core.Common;

namespace Pastarella.Core.Models;

public class AnalysisReport(DateTime timestamp)
{
    public uint Version = 1;

    public DateTime Timestamp = timestamp;

    public List<ProcessInfo> Processes = [];

    public List<ServiceInfo> Services = [];

    public List<PortInfo> OpenPorts = [];

    public List<UserInfo> Users = [];

    public List<Host> Hosts = [];

    public List<DriverInfo> Drivers = [];

    public List<PersistenceEntry> Persistences = [];

    public List<StorageInfo> Storages = [];

    public Dictionary<string, string> Envs = [];

    public List<CommandHistory> CommandHistories = [];

    public List<RecentFileInfo> RecentFiles = [];
}
