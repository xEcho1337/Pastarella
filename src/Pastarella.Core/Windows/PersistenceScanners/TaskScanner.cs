using Microsoft.Win32.TaskScheduler;
using Pastarella.Core.Models;
using ShowMessageAction = Microsoft.Win32.TaskScheduler.ShowMessageAction;

namespace Pastarella.Core.Windows.PersistenceScanners;

public class TaskScanner : IPersistenceScanner
{
    public IEnumerable<PersistenceEntry> Scan()
    {
        foreach (var task in ScanTasks())
        {
            foreach (var trigger in task.Triggers)
            {
                foreach (var action in task.Actions)
                {
                    IScheduledAction wrapped = action switch
                    {
                        ComHandlerAction com => new ComScheduledAction
                        {
                            ClassId = com.ClassId,
                            ClassName = com.ClassName
                        },
                        ExecAction exec => new ExecScheduledAction
                        {
                            Path = $"{exec.Path} {exec.Arguments}",
                            Sha256 = PlatformHelpers.GetSha256(exec.Path),
                        },
                        EmailAction => new EmailScheduledAction(),
                        ShowMessageAction message => new MessageScheduledAction()
                        {
                            Title = message.Title,
                            Message = message.MessageBody
                        },
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    // TODO: parse trigger
                    yield return new PersistenceEntry.ScheduledTask
                    {
                        Name = task.Name,
                        Path = task.PathName,
                        Action = wrapped,
                        Trigger = ExecutionTrigger.Scheduled,
                        Privilege = PersistencePrivilege.User, // TODO: check this
                        Type = PersistenceType.ScheduledTask,
                        Metadata =
                        {
                            ["State"] = task.State,
                            ["LastRunTime"] = task.LastRunTime,
                            ["NextRunTime"] = task.NextRunTime,

                            ["StartBoundary"] = trigger.StartBoundary,
                            ["Duration"] = trigger.Repetition.Duration.ToString(),

                            ["RegisteredBy"] = task.RegistrationInfo.Author,
                            ["RegisteredOn"] = task.RegistrationInfo.Date,
                            ["User"] = task.Principal.UserId,
                            ["LogonType"] = task.Principal.LogonType.ToString()
                        },
                        Schedule = trigger.Repetition.Interval.ToString(), // TODO: parse trigger
                    };
                }
            }
        }
    }


    public IEnumerable<TaskInfo> ScanTasks()
    {
        return TaskService.Instance.AllTasks
            .Select(t => new TaskInfo(
                t.Name,
                t.Path,
                t.State.ToString(),
                t.Definition.Principal,
                t.Definition.RegistrationInfo,
                t.NextRunTime,
                t.LastRunTime,
                t.Definition.Triggers,
                t.Definition.Actions)
            )
            .ToList();
    }
}

public record TaskInfo(
    string Name,
    string PathName,
    string State,
    TaskPrincipal Principal,
    TaskRegistrationInfo RegistrationInfo,
    DateTime NextRunTime,
    DateTime LastRunTime,
    TriggerCollection Triggers,
    ActionCollection Actions
);
