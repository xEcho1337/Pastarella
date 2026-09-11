using Pastarella.Core.Models;

namespace Pastarella.Core.MacOS;

public class UserScanner : IUserScanner
{
    public IEnumerable<UserInfo> Scan(IProgress<ScanProgress>? progress = null)
    {
        var users = new List<UserInfo>();
        try
        {
            string output = PlatformHelpers.RunProcessAndCaptureOutput("dscl", ". -list /Users");
            string[] names = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            int done = 0;

            foreach (string user in names)
            {
                if (string.IsNullOrWhiteSpace(user)) continue;
                if (user.StartsWith('_')) continue;

                string uniqueId = "";
                string homeDir = "";
                string realName = "";

                string commandOut =
                    PlatformHelpers.RunProcessAndCaptureOutput("dscl", $". -read /Users/{user} RealName UniqueID NFSHomeDirectory");

                string[] lines = commandOut.Split("\n");

                for (int j = 0; j < lines.Length; j++)
                {
                    string l = lines[j];
                    if (l.StartsWith("UniqueID"))
                        uniqueId = l.Replace("UniqueID: ", "").Trim();
                    if (l.StartsWith("NFSHomeDirectory"))
                        homeDir = l.Replace("NFSHomeDirectory: ", "").Trim();

                    if (l.StartsWith("RealName"))
                    {
                        string value = l.Replace("RealName:", "").Trim();

                        if (!string.IsNullOrWhiteSpace(value))
                            realName = value;
                        else if (j + 1 < lines.Length)
                            realName = lines[++j].Trim();
                    }
                }

                users.Add(new UserInfo(user, realName, uniqueId, homeDir, false));
                progress?.Report(new ScanProgress(++done, names.Length));
            }
        }
        catch
        {
            // ignore
        }

        return users;
    }
}
