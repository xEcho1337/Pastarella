using System.Diagnostics;
using Pastarella.Core.Models;

namespace Pastarella.Core.FreeBSD.PersistenceScanners;

public class ServiceScanner : IPersistenceScanner
{
    public IEnumerable<PersistenceEntry> Scan(IProgress<ScanProgress>? progress = null)
    {
        var cmd = Process.Start(new ProcessStartInfo()
        {
            FileName = "/usr/sbin/service",
            Arguments = "-e",
            RedirectStandardOutput = true,
        });

        if (cmd == null)
            return [];

        string output = cmd.StandardOutput.ReadToEnd();
        string[] lines = output[..^1].Split('\n'); // [..^1] is for trimming last '\n'

        var list = new List<PersistenceEntry>(lines.Length);

        foreach (string servicePath in lines)
        {
            list.Add(new()
            {
                Name = servicePath.Split('/')[^1],
                Path = servicePath,
                Action = new ExecScheduledAction()
                {
                    ExePath = null,
                },

                Type = PersistenceType.Service,

                Trigger = ExecutionTrigger.SystemStartup,
                Privilege = PersistencePrivilege.Admin,
            });
        }

        return list;
    }
}
