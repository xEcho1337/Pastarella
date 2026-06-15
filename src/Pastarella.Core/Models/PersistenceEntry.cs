using System.Text.Json.Serialization;

namespace Pastarella.Core.Models;

public class PersistenceEntry
{
    public required string Name { get; set; }

    public required string Path { get; set; }

    public IScheduledAction Action { get; set; }

    public ExecutionTrigger Trigger { get; set; }

    public required PersistencePrivilege Privilege { get; set; }

    public required PersistenceType Type { get; set; }

    public Dictionary<string, object> Metadata { get; set; } = [];

    public int RiskScore { get; set; }

    public class ScheduledTask : PersistenceEntry
    {
        public ScheduledTask()
        {
            Type = PersistenceType.ScheduledTask;
        }

        public required string Schedule { get; set; }
    }
}

[JsonDerivedType(typeof(ExecScheduledAction))]
[JsonDerivedType(typeof(ComScheduledAction))]
[JsonDerivedType(typeof(EmailScheduledAction))]
[JsonDerivedType(typeof(MessageScheduledAction))]
public interface IScheduledAction;

public class ExecScheduledAction : IScheduledAction
{
    public required string Path { get; set; }
    public string? Sha256 { get; set; }
}

public class ComScheduledAction : IScheduledAction
{
    public required Guid ClassId { get; set; }

    public required string ClassName { get; set; }
}

// TODO:
public class EmailScheduledAction : IScheduledAction;

public class MessageScheduledAction : IScheduledAction
{
    public required string Title { get; set; }

    public required string Message { get; set; }
}
