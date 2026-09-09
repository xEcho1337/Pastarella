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
        var ctx = new Context();
        var report = new AnalysisReport(DateTime.UtcNow);

        Dictionary<string, Action<IProgress<ScanProgress>>> actions = new()
        {
            ["Environment Variables"] = p => report.Envs = EnvironmentVariablesScanner.GetEnvs(),
            ["Hosts"] = p => report.Hosts = HostsScanner.GetHosts().ToList(),
            ["Recent Files"] = p => report.RecentFiles = RecentFileScanner.Scan(p).ToList(),
        };

        if (ctx.ForensicScanner != null)
        {
            actions.Add("Processes", p => report.Processes = ctx.ForensicScanner.ScanProcesses(p).ToList());
            actions.Add("Users", p => report.Users = ctx.ForensicScanner.ScanUsers(p).ToList());
            actions.Add("Storages", p => report.Storages = ctx.ForensicScanner.ScanStorages().ToList());
        }

        if (ctx.PersistenceScanner != null)
            actions.Add("Persistence checks", p => report.Persistences = ctx.PersistenceScanner.Scan(p).ToList());
        if (ctx.NetworkScanner != null)
            actions.Add("Open connections", p => report.OpenPorts = ctx.NetworkScanner.Scan(p).ToList());
        if (ctx.DriverScanner != null)
            actions.Add("Drivers", p => report.Drivers = ctx.DriverScanner.Scan(p).ToList());
        if (ctx.ServiceScanner != null)
            actions.Add("Services", p => report.Services = ctx.ServiceScanner.Scan(p).ToList());
        if (ctx.CommandHistoryScanner != null)
            actions.Add("Command histories", p => report.CommandHistories = ctx.CommandHistoryScanner.Scan(p).ToList());

        Start(report, actions);
        return 0;
    }

    public static void Start(AnalysisReport report, Dictionary<string, Action<IProgress<ScanProgress>>> actions)
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

    private static void RunAnalysis(List<string> checks, Dictionary<string, Action<IProgress<ScanProgress>>> actions)
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

                using var gate = new SemaphoreSlim(3);
                foreach (string check in checks)
                {
                    var task = tasks[check];
                    var work = Task.Run(async () =>
                    {
                        await gate.WaitAsync();

                        try
                        {
                            task.StartTask();

                            // Hybrid bar: creep toward 90% while the total is
                            // unknown; switch to the real percentage as soon
                            // as the scanner reports a total.
                            int hasTotal = 0;
                            var progress = new Progress<ScanProgress>(p =>
                            {
                                if (p.Total is > 0)
                                {
                                    Interlocked.Exchange(ref hasTotal, 1);
                                    task.MaxValue = p.Total.Value;
                                    task.Value = Math.Min(p.Done, p.Total.Value);
                                }

                                task.Description = string.IsNullOrWhiteSpace(p.Phase)
                                    ? Volatile.Read(ref hasTotal) == 0
                                        ? $"{check} [grey]({p.Done:N0})[/]"
                                        : check
                                    : $"{check} [grey]({p.Phase})[/]";
                            });

                            using var pulseCts = new CancellationTokenSource();
                            var pulse = Task.Run(async () =>
                            {
                                try
                                {
                                    while (!pulseCts.Token.IsCancellationRequested)
                                    {
                                        await Task.Delay(200, pulseCts.Token);

                                        if (Volatile.Read(ref hasTotal) == 0 && task.Value < 90)
                                            task.Increment(1.5);
                                    }
                                }
                                catch (OperationCanceledException)
                                {
                                    // expected on cancel
                                }
                            });

                            try
                            {
                                actions[check](progress);
                            }
                            finally
                            {
                                await pulseCts.CancelAsync();
                                await pulse;
                            }

                            task.Value = task.MaxValue;
                            task.StopTask();
                            totalTask.Increment(1);
                        }
                        catch (Exception e)
                        {
                            task.Description = $"[bold red]✗ {task.Description}[/]";
                            task.StopTask();
                            errors.Add(e);
                            totalTask.Increment(1);
                        }
                        finally
                        {
                            gate.Release();
                        }
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
        AnsiConsole.WriteLine();

        if (errors.IsEmpty) return;

        AnsiConsole.MarkupLine("[red]Error reports:[/]");

        foreach (var exception in errors)
            AnsiConsole.WriteException(exception);
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
