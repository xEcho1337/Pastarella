using Pastarella.Core.Models;

namespace Pastarella.Terminal.Outputs.Txt;

public class PersistenceScanner(OutputBuffer buffer)
{
    private readonly OutputBuffer Buffer = buffer;

    public void WritePersistences(IEnumerable<PersistenceEntry> persistences)
    {
        Buffer.WriteLine("Risk score [Type | Trigger] (Privilege) Name -> Path");
        foreach (var persistence in persistences)
        {
            Buffer.WriteLine($"{persistence.RiskScore} [{persistence.Type} | {persistence.Trigger}] ({persistence.Privilege}) {persistence.Name} -> {persistence.Path}");

            switch (persistence)
            {
                case PersistenceEntry.ScheduledTask sched:
                    Buffer.WriteLine($"  |> Scheduled at: {sched.Schedule}");
                    break;
            }

            Buffer.WriteLine($"  |> Action type: {persistence.Action.GetType().Name}");
            switch (persistence.Action)
            {
                case ExecScheduledAction exec:
                    Buffer.WriteLine($"  |>  File: {exec.Path} [{exec.Sha256 ?? "N/A"}]");
                    break;
                case ComScheduledAction com:
                    Buffer.WriteLine($"  |>  Class name: {com.ClassName}");
                    Buffer.WriteLine($"  |>  Class ID: {com.ClassId}");
                    break;
                case EmailScheduledAction email:
                    throw new NotImplementedException();
                case MessageScheduledAction message:
                    Buffer.WriteLine($"  |>  Title: {message.Title}");
                    Buffer.WriteLine($"  |>  Message: {message.Message}");
                    break;
            }

            TxtWriter.BasicPrintMetadata(Buffer, persistence.Metadata);
        }
    }
}

