using Pastarella.Core.Models;
using Pastarella.Terminal.Outputs.Txt;

namespace Pastarella.Terminal.Outputs;

public class TxtWriter
{
    private readonly OutputBuffer _buffer = new();

    public static void BasicPrintMetadata(OutputBuffer buffer, Dictionary<string, object> dict)
    {
        if (dict.Count == 0)
            return;

        buffer.WriteLine("Metadata:");
        buffer.Indent();
        foreach (var (k, v) in dict)
            buffer.WriteLine($"|> {k}: {v}");
        buffer.Unindent();
    }

    private void PrintTitle(string title)
    {
        _buffer.WriteLine($"============ {title} ============\n");
    }

    public string Print(AnalysisReport report)
    {
        _buffer.WriteLine($"###### Report timestamp: {report.Timestamp} ######");

        var forensic = new ForensicServices(_buffer);
        PrintTitle("Processes");
        forensic.WriteProcesses(report.Processes);

        PrintTitle("Services");
        forensic.WriteServices(report.Services);

        PrintTitle("Users");
        forensic.WriteUsers(report.Users);

        PrintTitle("Storages");
        forensic.WriteStorages(report.Storages);

        var network = new NetworkService(_buffer);
        PrintTitle("Open connections");
        network.WritePorts(report.OpenPorts);

        var hosts = new HostsScanner(_buffer);
        PrintTitle($"Hosts file ({Core.Common.HostsScanner.Path})");
        hosts.WriteHosts(report.Hosts);

        var persistences = new PersistenceScanner(_buffer);
        PrintTitle("Persistences");
        persistences.WritePersistences(report.Persistences);

        var drivers = new DriverScanner(_buffer);
        PrintTitle("Drivers");
        drivers.WriteDrivers(report.Drivers);

        var envs = new EnvironmentVariablesScanner(_buffer);
        PrintTitle("Environment variables");
        envs.WriteEnvs(report.Envs);

        var cmdHistories = new CommandHistoryScanner(_buffer);
        PrintTitle("Command Histories");
        cmdHistories.WriteHistories(report.CommandHistories);

        return _buffer.Text;
    }
}

