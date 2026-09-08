using Pastarella.Core.Models;

namespace Pastarella.Terminal.Outputs.Txt;

public class RecentFileScanner(OutputBuffer buffer)
{
    private readonly OutputBuffer Buffer = buffer;

    public void Write(IEnumerable<RecentFileInfo> files)
    {
        foreach (var file in files)
        {
            Buffer.WriteLine(file.FilePath);
            Buffer.WriteLine($"  > Creation time: {file.CreationTime}");
            Buffer.WriteLine($"  > Last write time: {file.LastWriteTime}");
        }
    }
}
