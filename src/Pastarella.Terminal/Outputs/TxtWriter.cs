using Pastarella.Core;
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

    public static void PrintExePath(OutputBuffer buffer, ExePath? _exePath)
    {
        if (_exePath is not ExePath exePath)
            return;

        buffer.WriteLine($"Executable path: {exePath.NormalizedValue}");
        buffer.Indent();

        if (exePath.Sha256 is string hash)
            buffer.WriteLine($"|> SHA256: {hash}");

        if (exePath.Signature is ISignature signature)
        {
            buffer.WriteLine("|> Signature:");
            switch (signature)
            {
                case WindowsSignature sign:
                    buffer.WriteLine($"|> Issuer: {sign.Issuer}");
                    buffer.WriteLine($"|> Subject: {sign.Subject}");
                    break;
                case MacOSSignature sign:
                    if (sign.TeamIdentifier is string teamId)
                        buffer.WriteLine($"|> Team Identifier: {teamId}");
                    if (sign.Autority is string autority)
                        buffer.WriteLine($"|> Autority: {autority}");
                    break;
                default:
                    break;
            }
        }
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

        if (report.Processes.Count != 0)
        {
            PrintTitle("Processes");
            forensic.WriteProcesses(report.Processes);
        }

        if (report.Services.Count != 0)
        {
            PrintTitle("Services");
            forensic.WriteServices(report.Services);
        }

        if (report.Users.Count != 0)
        {
            PrintTitle("Users");
            forensic.WriteUsers(report.Users);
        }

        if (report.Storages.Count != 0)
        {
            PrintTitle("Storages");
            forensic.WriteStorages(report.Storages);
        }

        if (report.OpenPorts.Count != 0)
        {
            var network = new NetworkService(_buffer);
            PrintTitle("Open connections");
            network.WritePorts(report.OpenPorts);
        }

        if (report.Hosts.Count != 0)
        {
            var hosts = new HostsScanner(_buffer);
            PrintTitle($"Hosts file ({Core.Common.HostsScanner.Path})");
            hosts.WriteHosts(report.Hosts);
        }

        if (report.Persistences.Count != 0)
        {
            var persistences = new PersistenceScanner(_buffer);
            PrintTitle("Persistences");
            persistences.WritePersistences(report.Persistences);
        }

        if (report.Drivers.Count != 0)
        {
            var drivers = new DriverScanner(_buffer);
            PrintTitle("Drivers");
            drivers.WriteDrivers(report.Drivers);
        }

        if (report.Envs.Count != 0)
        {
            var envs = new EnvironmentVariablesScanner(_buffer);
            PrintTitle("Environment variables");
            envs.WriteEnvs(report.Envs);
        }

        if (report.CommandHistories.Count != 0)
        {
            var cmdHistories = new CommandHistoryScanner(_buffer);
            PrintTitle("Command Histories");
            cmdHistories.WriteHistories(report.CommandHistories);
        }

        if (report.RecentFiles.Count != 0)
        {
            var recentFiles = new RecentFileScanner(_buffer);
            PrintTitle("Recent Files (last 30 days)");
            recentFiles.Write(report.RecentFiles);
        }

        if (report.Containers.Count != 0)
        {
            var containers = new ContainerScanner(_buffer);
            PrintTitle("Containers");
            containers.Write(report.Containers);
        }

        return _buffer.Text;
    }
}
