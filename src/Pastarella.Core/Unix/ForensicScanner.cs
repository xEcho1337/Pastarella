using Pastarella.Core.Models;

namespace Pastarella.Core.Unix;

public class ForensicScanner : IForensicScanner
{
    public IEnumerable<ProcessInfo> ScanProcesses()
    {
        throw new Exception("Scanning processes is OS-specific, not a UNIX \"standard\"");
    }

    public IEnumerable<UserInfo> ScanUsers()
    {
        List<UserInfo> list = [];

        foreach (string line in File.ReadAllLines("/etc/passwd"))
        {
            string[] parts = line.Split(':');

            string name = parts[0];
            string uid = parts[2];
            string gid = parts[3];
            string gecos = parts[4];
            string home = parts[5];
            string shell = parts[6];

            bool is_disabled = shell.EndsWith("/nologin") || shell.EndsWith("/false");

            list.Add(new(name, "", uid, home, is_disabled)
            {
                Metadata =
                {
                    ["gecos"] = gecos,
                    ["gid"] = gid,
                    ["shell"] = shell,
                },
            });
        }

        return list;
    }
}

