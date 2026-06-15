namespace Pastarella.Core.Common;

public static class FileScanner
{
    public static IEnumerable<FileInfo> GetRecentFiles(string rootPath, int days = 30)
    {
        var limit = DateTime.Now.AddDays(-days);

        foreach (string file in Directory.EnumerateFiles(rootPath, "*", SearchOption.AllDirectories))
        {
            FileInfo? info = null;

            try
            {
                info = new FileInfo(file);

                if (info.LastWriteTime >= limit)
                    info = null;
            }
            catch
            {
                // ignore
            }

            if (info != null)
                yield return info;
        }
    }
}
