using Pastarella.Core.Models;

namespace Pastarella.Core.Unix;

public class CommandHistoryScanner : ICommandHistoryScanner
{
    public IEnumerable<CommandHistory> Scan(IProgress<ScanProgress>? progress = null)
    {
        string home = Environment.GetEnvironmentVariable("HOME") ?? throw new Exception("HOME env not found");

        if (File.Exists($"{home}/.ash_history"))
            yield return new("ash", File.ReadAllLines($"{home}/.ash_history"));

        if (File.Exists($"{home}/.bash_history"))
            yield return new("bash", File.ReadAllLines($"{home}/.bash_history"));

        if (File.Exists($"{home}/.zsh_history"))
            yield return new("zsh", File.ReadAllLines($"{home}/.zsh_history"));
    }
}
