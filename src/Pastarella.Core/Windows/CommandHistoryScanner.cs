using Pastarella.Core.Models;

namespace Pastarella.Core.Windows;

public class CommandHistoryScanner : ICommandHistoryScanner
{
    private static CommandHistory GetPowerShellHistory()
    {
        string historyFilePath = $"{Environment.GetEnvironmentVariable("APPDATA")}\\Microsoft\\Windows\\PowerShell\\PSReadLine\\ConsoleHost_history.txt";
        return new("PowerShell", File.ReadAllLines(historyFilePath));
    }

    public IEnumerable<CommandHistory> Scan()
    {
        return [GetPowerShellHistory()];
    }
}
