using System.ComponentModel;
using Pastarella.Core;
using Pastarella.Core.Models;
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

        [CommandOption("-r|--recent-files")]
        [Description("Show all recent files")]
        [DefaultValue(false)]
        public bool RecentFiles { get; init; }

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

        [CommandOption("-C|--command-histories")]
        [Description("Show all command histories")]
        [DefaultValue(false)]
        public bool CommandHistories { get; init; }

        [CommandArgument(0, "<output>")]
        [Description("Name of the output file")]
        public required string Output { get; init; }
    }

    public class NotImplementedPlatformException : Exception
    {
        public NotImplementedPlatformException(string scannerName)
            : base($"{scannerName} scanner is not implemented for this platform")
        {
        }
    }

    protected override int Execute(CommandContext context, CliSettings settings, CancellationToken cancellationToken)
    {
        if (!PlatformHelpers.IsElevated())
            Console.WriteLine("Program not executed with admin privileges. Not all information will be given");

        try
        {
            var ctx = new Context();
            var report = new AnalysisReport(DateTime.UtcNow);

            if (settings.Envs)
                report.Envs = EnvironmentVariablesScanner.GetEnvs();
            if (settings.Hosts)
                report.Hosts = HostsScanner.GetHosts().ToList();
            if (settings.RecentFiles)
                report.RecentFiles = RecentFileScanner.Scan().ToList();

            if ((settings.Users || settings.Storages || settings.Processes) && ctx.ForensicScanner == null)
                throw new NotImplementedException("Forensic");

            if (settings.Users)
                report.Users = ctx.ForensicScanner!.ScanUsers().ToList();
            if (settings.Storages)
                report.Storages = ctx.ForensicScanner!.ScanStorages().ToList();
            if (settings.Processes)
                report.Processes = ctx.ForensicScanner!.ScanProcesses().ToList();

            if (settings.Persistances)
            {
                if (ctx.PersistenceScanner == null)
                    throw new NotImplementedException("Persistence");
                report.Persistences = ctx.PersistenceScanner.Scan().ToList();
            }

            if (settings.OpenConnections)
            {
                if (ctx.NetworkScanner == null)
                    throw new NotImplementedPlatformException("Network");
                report.OpenPorts = ctx.NetworkScanner.Scan().ToList();
            }

            if (settings.Drivers)
            {
                if (ctx.DriverScanner == null)
                    throw new NotImplementedPlatformException("Driver");
                report.Drivers = ctx.DriverScanner.Scan().ToList();
            }

            if (settings.Services)
            {
                if (ctx.ServiceScanner == null)
                    throw new NotImplementedPlatformException("Service");
                report.Services = ctx.ServiceScanner.Scan().ToList();
            }

            if (settings.CommandHistories)
            {
                if (ctx.CommandHistoryScanner == null)
                    throw new NotImplementedPlatformException("Command History");
                report.CommandHistories = ctx.CommandHistoryScanner.Scan().ToList();
            }

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
