using Pastarella.Core;
using Pastarella.Core.Models;

namespace Pastarella.Core.Windows.PersistenceScanners;

public class UsersLogonScript : IPersistenceScanner
{
    public IEnumerable<PersistenceEntry> Scan(IProgress<ScanProgress>? progress = null)
    {
        return ForensicScanner.CachedUserInfo.Where(u => !string.IsNullOrEmpty(u.Item1.usri1_script_path)).Select(u =>
        {
            var (info, _) = u;

            return new PersistenceEntry
            {
                Name = Path.GetFileName(info.usri1_script_path)!,
                Path = info.usri1_script_path!,
                Action = new ExecScheduledAction()
                {
                    Path = info.usri1_script_path,
                    Sha256 = PlatformHelpers.GetSha256(info.usri1_script_path),
                },
                Privilege = PersistencePrivilege.User,
                Type = PersistenceType.ScheduledTask,
            };
        });
    }
}
