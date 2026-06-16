using Pastarella.Core.Models;

namespace Pastarella.Terminal.Outputs.Txt;

public class CommandHistoryScanner(OutputBuffer buffer)
{
    private readonly OutputBuffer Buffer = buffer;

    public void WriteHistories(IEnumerable<CommandHistory> histories)
    {
        foreach (CommandHistory hist in histories)
        {
            Buffer.WriteLine(hist.Shell);
            foreach (string cmd in hist.Commands)
                Buffer.WriteLine($"  > {cmd}");
        }
    }
}
