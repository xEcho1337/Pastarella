namespace Pastarella.Core.Models;

public record CommandHistory(string Shell, IEnumerable<string> Commands);

public interface ICommandHistoryScanner
{
    IEnumerable<CommandHistory> Scan(IProgress<ScanProgress>? progress = null);
}
