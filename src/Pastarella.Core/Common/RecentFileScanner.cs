using Pastarella.Core.Models;

namespace Pastarella.Core.Common;

public static class RecentFileScanner
{
    private static List<RecentFileInfo> GetFilesOfDir(string dir, DateTime limit, bool recursive = true)
    {
        List<RecentFileInfo> files = [];

        foreach (string file in Directory.EnumerateFiles(dir, "*", new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = recursive,
        }))
        {
            try
            {
                FileInfo info = new(file);
                if (info.LastWriteTime >= limit && info.CreationTime >= limit)
                    files.Add(new(file, info.CreationTime, info.LastWriteTime));
            }
            catch
            {
                // ignore
            }
        }

        return files;
    }

    public static IEnumerable<RecentFileInfo> Scan()
    {
        var limit = DateTime.Now.AddDays(-30);

        if (OperatingSystem.IsWindows())
        {
            // TODO: do with all disks
            return GetFilesOfDir("C:", limit);
        }
        else
        {
            List<RecentFileInfo> files = [];

            files.AddRange(GetFilesOfDir("/", limit, false));
            foreach (string dir in Directory.EnumerateDirectories("/", "*", new EnumerationOptions { IgnoreInaccessible = true }))
            {
                if (dir == "/proc" || dir == "/sys" || dir == "/dev")
                    continue;

                files.AddRange(GetFilesOfDir(dir, limit));
            }

            return files;
        }
    }
}
