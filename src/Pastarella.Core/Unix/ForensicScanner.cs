using Pastarella.Core.Models;

namespace Pastarella.Core.Unix;

public class ForensicScanner : IForensicScanner
{
    public IEnumerable<ProcessInfo> ScanProcesses(IProgress<ScanProgress>? progress = null)
    {
        throw new Exception("Scanning processes is OS-specific, not a UNIX \"standard\"");
    }

    public IEnumerable<UserInfo> ScanUsers(IProgress<ScanProgress>? progress = null)
    {
        List<UserInfo> list = [];
        string[] lines = File.ReadAllLines("/etc/passwd");
        int done = 0;

        foreach (string line in lines)
        {
            string[] parts = line.Split(':');

            string name = parts[0];
            string uid = parts[2];
            string gid = parts[3];
            string gecos = parts[4];
            string home = parts[5];
            string shell = parts[6];

            string full_name = gecos.Split(',', 2)[0];
            bool is_disabled = shell.EndsWith("/nologin") || shell.EndsWith("/false");

            list.Add(new(name, full_name, uid, home, is_disabled)
            {
                Metadata =
                {
                    ["gecos"] = gecos,
                    ["gid"] = gid,
                    ["shell"] = shell,
                },
            });

            progress?.Report(new ScanProgress(++done, lines.Length));
        }

        return list;
    }
}

