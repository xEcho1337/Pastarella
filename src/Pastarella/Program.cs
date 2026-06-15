using Pastarella.Terminal.Commands;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Pastarella;

internal static class Program
{
    public static int Main(string[] args)
    {
        // If started by double-clicking (no arguments), default to the TUI command
        if (args.Length == 0) args = ["tui"];

        var app = new CommandApp();

        app.Configure(config =>
        {
            config.AddCommand<CliCommand>("cli");
            config.AddCommand<TuiCommand>("tui");

            config.SetExceptionHandler((ex, _) =>
            {
                AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths);
                return 1;
            });
        });

        return app.Run(args);
    }
}
