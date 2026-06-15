using System.ComponentModel;
using Pastarella.Core.Common;
using Pastarella.Terminal.Outputs;
using Spectre.Console.Cli;

namespace Pastarella.Terminal.Commands;

public class CliCommand : Command<CliCommand.CliSettings>
{
    public class CliSettings : CommandSettings
    {
        [CommandOption("-e|--envs")]
        [Description("Show all environment variables")]
        [DefaultValue(false)]
        public bool Envs { get; init; }

        [CommandOption("-H|--hosts")]
        [Description("Show all entries in etc/hosts")]
        [DefaultValue(false)]
        public bool Hosts { get; init; }

        [CommandOption("-d|--drivers")]
        [Description("Show all used drivers")]
        [DefaultValue(false)]
        public bool Drivers { get; init; }

        [CommandOption("-p|--processes")]
        [Description("Show all running processes")]
        [DefaultValue(false)]
        public bool Processes { get; init; }

        [CommandOption("-s|--services")]
        [Description("Show all services")]
        [DefaultValue(false)]
        public bool Services { get; init; }

        [CommandOption("-u|--users")]
        [Description("Show all users")]
        [DefaultValue(false)]
        public bool Users { get; init; }

        [CommandOption("-S|--storages")]
        [Description("Show all storages")]
        [DefaultValue(false)]
        public bool Storages { get; init; }

        [CommandOption("-c|--connections")]
        [Description("Show all active connections")]
        [DefaultValue(false)]
        public bool OpenConnections { get; init; }

        [CommandOption("-P|--persistance")]
        [Description("Show all persistances")]
        [DefaultValue(false)]
        public bool Persistances { get; init; }

        [CommandArgument(0, "<output>")]
        [Description("Name of the output file")]
        public required string Output { get; init; }
    }

    protected override int Execute(CommandContext context, CliSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            var dispatcher = ExecutionContext.Dispatcher;
            var forensic = ExecutionContext.ForensicScanner;
            var persistence = ExecutionContext.PersistenceScanner;
            var network = ExecutionContext.NetworkScanner;
            var driver = ExecutionContext.DriverScanner;
            var service = ExecutionContext.ServiceScanner;

            var report = ExecutionContext.Dispatcher.Report;

            if (settings.Envs) report.Envs = EnvironmentVariablesScanner.GetEnvs();
            if (settings.Hosts) report.Hosts = HostsScanner.GetHosts().ToList();
            if (settings.Drivers) report.Drivers = driver.Scan().ToList();
            if (settings.Processes) report.Processes = forensic.ScanProcesses().ToList();
            if (settings.Services) report.Services = service.Scan().ToList();
            if (settings.Users) report.Users = forensic.ScanUsers().ToList();
            if (settings.Storages) report.Storages = forensic.ScanStorages().ToList();
            if (settings.OpenConnections) report.OpenPorts = network.Scan().ToList();
            if (settings.Persistances) report.Persistences = persistence.Scan().ToList();

            if (settings.Output.EndsWith(".json"))
                File.WriteAllText(settings.Output, JsonWriter.Serialize(report));
            else
                File.WriteAllText(settings.Output, new TxtWriter().Print(report));

            Console.WriteLine("Output saved in " + settings.Output);
            return 0;
        }
        catch
        {
            return -1;
        }
    }
}
