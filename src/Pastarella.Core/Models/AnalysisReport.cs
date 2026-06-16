namespace Pastarella.Core.Models;

public class AnalysisReport
{
    public DateTime Timestamp;

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
}
