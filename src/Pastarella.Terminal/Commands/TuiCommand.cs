using System.Collections.Concurrent;
using Pastarella.Core.Common;
using Pastarella.Core.Models;
using Spectre.Console;
using Spectre.Console.Cli;
using Pastarella.Terminal.Outputs;
using Pastarella.Core;

namespace Pastarella.Terminal.Commands;

public class TuiCommand : Command<TuiCommand.TuiSettings>
{
    public class TuiSettings : CommandSettings;

    protected override int Execute(CommandContext context, TuiSettings settings, CancellationToken cancellationToken)
    {
        var dispatcher = ExecutionContext.Dispatcher;
        var forensic = ExecutionContext.ForensicScanner;
        var persistence = ExecutionContext.PersistenceScanner;
        var network = ExecutionContext.NetworkScanner;
        var driver = ExecutionContext.DriverScanner;
        var service = ExecutionContext.ServiceScanner;
        var cmdHistory = ExecutionContext.CommandHistoryScanner;

        var report = dispatcher.Report;

        Dictionary<string, Action> actions = new()
        {
            ["Environment Variables"] = () => report.Envs = EnvironmentVariablesScanner.GetEnvs(),
            ["Hosts"] = () => report.Hosts = HostsScanner.GetHosts().ToList(),
            ["Recent Files"] = () => report.RecentFiles = RecentFileScanner.Scan().ToList(),
            ["Drivers"] = () => report.Drivers = driver.Scan().ToList(),
            ["Processes"] = () => report.Processes = forensic.ScanProcesses().ToList(),
            ["Services"] = () => report.Services = service.Scan().ToList(),
            ["Users"] = () => report.Users = forensic.ScanUsers().ToList(),
            ["Storages"] = () => report.Storages = forensic.ScanStorages().ToList(),
            ["Open Connections"] = () => report.OpenPorts = network.Scan().ToList(),
            ["Persistence Checks"] = () => report.Persistences = persistence.Scan().ToList(),
            ["Command Histories"] = () => report.CommandHistories = cmdHistory.Scan().ToList(),
        };

        dispatcher.AddDispatchers(actions);

        Start(report, actions);
        return 0;
    }

    public static void Start(AnalysisReport report, Dictionary<string, Action> actions)
    {
        AnsiConsole.MarkupLine("[bold yellow]PASTARELLA[/]");
        if (!PlatformHelpers.IsElevated())
            AnsiConsole.MarkupLine("[bold red]Program not executed with admin privileges. Not all information will be given[/]");

        var checks = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("[grey]Select the options[/]")
                .InstructionsText("[grey](Press [blue]<space>[/] to select, [green]<enter>[/] to confirm)[/]")
                .AddChoices(actions.Keys)
            );

        report.Timestamp = DateTime.UtcNow;
        RunAnalysis(checks, actions);
        Export(report);

        AnsiConsole.Prompt(new TextPrompt<string>("[grey]Press [green]<enter>[/] to exit...[/]"));
        AnsiConsole.MarkupLine("[yellow]Bye![/] Thanks for using Pastarella [red]♥[/]");
    }

    private static void RunAnalysis(List<string> checks, Dictionary<string, Action> actions)
    {
        var errors = new ConcurrentBag<Exception>();

        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold yellow]PASTARELLA[/]");
        AnsiConsole.MarkupLine("[grey]Analysis in progress...[/]");
        AnsiConsole.WriteLine();

        DateTime start = DateTime.UtcNow, end = DateTime.UtcNow;

        AnsiConsole.Progress()
            .Columns(
                new SpinnerColumn(),
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new ElapsedTimeColumn())
            .StartAsync(async ctx =>
            {
                var totalTask = ctx.AddTask("Complete analysis", maxValue: checks.Count);

                var tasks = checks.ToDictionary(
                    check => check,
                    check => ctx.AddTask(check, false, 100));

                var working = new List<Task>();
                start = DateTime.UtcNow;

                foreach (string check in checks)
                {
                    var task = tasks[check];
                    var work = Task.Run(() =>
                    {
                        task.StartTask();

                        try
                        {
                            actions[check]();
                            task.Increment(100);
                        }
                        catch (Exception e)
                        {
                            task.Description = $"[bold red]✗ {task.Description}[/]";
                            task.StopTask();
                            errors.Add(e);
                        }

                        totalTask.Increment(1);
                    });

                    working.Add(work);
                }

                await Task.WhenAll(working);

                end = DateTime.UtcNow;
                totalTask.Value = totalTask.MaxValue;
            }).GetAwaiter().GetResult();

        var took = end - start;

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[green]Analysis completed in {took.TotalSeconds:F3}s.[/]");
        AnsiConsole.MarkupLine($"[grey]Cached [green]{PlatformHelpers.CacheHits}[/] hashes[/]");
        AnsiConsole.WriteLine();

        if (!errors.IsEmpty)
        {
            AnsiConsole.MarkupLine("[red]Error reports:[/]");

            foreach (var exception in errors)
                AnsiConsole.WriteException(exception);
        }
    }

    private static void Export(AnalysisReport report)
    {
        string outputChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Visualize data")
                .AddChoices("Print in the terminal", "Export in TXT", "Export in JSON"));

        switch (outputChoice)
        {
            case "Print in the terminal":
                {
                    var txt = new TxtWriter();
                    Console.WriteLine(txt.Print(report));
                    break;
                }
            case "Export in TXT":
                {
                    string name = AskName(".txt");

                    var txt = new TxtWriter();

                    File.WriteAllText(name, txt.Print(report));
                    AnsiConsole.MarkupLine($"Saved the result in [green]{name}[/]");

                    break;
                }
            case "Export in JSON":
                {
                    string name = AskName(".json");

                    File.WriteAllText(name, JsonWriter.Serialize(report));
                    AnsiConsole.MarkupLine($"Saved the result in [green]{name}[/]");

                    break;
                }
        }
    }

    private static string AskName(string suffix)
    {
        while (true)
        {
            string name = AnsiConsole.Ask<string>("Insert the [green]file[/] name:");

            if (!name.EndsWith(suffix))
                name += suffix;

            if (!File.Exists(name))
                return name;

            bool res = AnsiConsole.Confirm($"A file with the name [blue]{name}[/] already exists, do you want to [red]overwrite[/] it?");

            if (!res) continue;

            return name;
        }
    }
}
