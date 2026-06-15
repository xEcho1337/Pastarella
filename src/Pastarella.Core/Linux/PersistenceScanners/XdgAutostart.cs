using Pastarella.Core.Models;

namespace Pastarella.Core.Linux.PersistenceScanners;

public class XdgAutostart : IPersistenceScanner
{
    private static IEnumerable<string> GetXdgAutostartPaths()
    {
        // TODO: handle XDG variables.
        // So, system-wide: $XDG_CONFIG_DIRS/autostart
        //     user-specific: $XDG_CONFIG_HOME/autostart

        yield return "/etc/xdg/autostart";

        foreach (string userHome in ForensicScanner.CachedUsersInfo.Select(u => u.Home))
            yield return Path.Combine(userHome, ".config/autostart");
    }

    public IEnumerable<PersistenceEntry> Scan()
    {
        List<PersistenceEntry> list = [];

        foreach (string dir in GetXdgAutostartPaths())
        {
            try
            {
                foreach (string desktopFile in Directory.GetFiles(dir))
                {
                    if (!desktopFile.EndsWith(".desktop"))
                        continue;

                    string? name = null;
                    string? exec = null;

                    foreach (string line in File.ReadAllLines(desktopFile))
                    {
                        if (string.IsNullOrEmpty(line) || line[0] == '#')
                            continue;

                        string[] parts = line.Split('=', 2);
                        if (parts.Length < 2)
                            continue;

                        string k = parts[0];
                        string v = parts[1];

                        if (k == "Name")
                            name = v;
                        else if (k == "Exec")
                            exec = v;
                    }

                    if (name == null || exec == null)
                        continue;

                    string execPath = PlatformHelpers.FindExecutableInPath(exec) ?? throw new Exception("Cannot be null");
                    list.Add(new PersistenceEntry()
                    {
                        Name = name,
                        Path = desktopFile,
                        Action = new ExecScheduledAction()
                        {
                            Path = execPath,
                            Sha256 = PlatformHelpers.GetSha256(execPath),
                        },
                        Trigger = ExecutionTrigger.UserLogin,
                        Privilege = PersistencePrivilege.User,
                        Type = PersistenceType.StartupFolder,
                    });
                }
            }
            catch (UnauthorizedAccessException)
            {
                // ignore
            }
            catch (DirectoryNotFoundException)
            {
                // Some users does not have an existent directory
            }
        }

        return list;
    }
}
