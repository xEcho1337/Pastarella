using System.Collections.Concurrent;
using Pastarella.Core.Models;

namespace Pastarella.Core.Common;

public static class RecentFileScanner
{
    private static readonly EnumerationOptions DefaultOptions = new() { IgnoreInaccessible = true };

    private static List<RecentFileInfo> GetFilesOfDir(string dir, DateTime limit)
    {
        List<RecentFileInfo> files = [];

        foreach (string file in GetAllFilesRecursive(dir))
        {
            try
            {
                var info = new FileInfo(file);
                if (info.LastWriteTime >= limit && info.CreationTime >= limit)
                    files.Add(new RecentFileInfo(file, info.CreationTime, info.LastWriteTime));
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

        List<RecentFileInfo> files = [];

        if (OperatingSystem.IsMacOS())
        {
            // Full-disk scan on macOS exhausts the default fd limit (256) and
            // crawls /System, /private, /Library: scan user homes instead.
            string usersRoot = "/Users";
            if (!Directory.Exists(usersRoot))
            {
                foreach (string dir in Directory.EnumerateDirectories(usersRoot))
                    files.AddRange(GetFilesOfDir(dir, limit));
            }
            else
            {
                files.AddRange(GetFilesOfDir("/", limit));
            }

            return files;
        }

        files.AddRange(GetFilesOfDir("/", limit));
        foreach (string dir in Directory.EnumerateDirectories("/", "*", new EnumerationOptions { IgnoreInaccessible = true }))
        {
            if (dir == "/proc" || dir == "/sys" || dir == "/dev")
                continue;

            files.AddRange(GetFilesOfDir(dir, limit));
        }

        return files;
    }

    static IEnumerable<string> SafeFiles(DirectoryInfo dir)
    {
        try
        {
            return dir.EnumerateFiles("*", DefaultOptions)
                .Select(f => f.FullName).ToList();
        }
        catch
        {
            return [];
        }
    }

    static DirectoryInfo[] SafeDirs(DirectoryInfo dir)
    {
        try
        {
            return dir.GetDirectories("*", DefaultOptions);
        }
        catch
        {
            return [];
        }
    }

    static IEnumerable<string> FilesOf(DirectoryInfo dir)
    {
        foreach (string f in SafeFiles(dir))
            yield return f;
    }

    static IEnumerable<string> GetAllFilesRecursive(string root)
    {
        int degreeOfParallelism = Math.Clamp(Environment.ProcessorCount, 1, 8);
        var rootDir = new DirectoryInfo(root);

        foreach (string f in FilesOf(rootDir))
            yield return f;

        var seen = new ConcurrentDictionary<string, byte>();
        seen.TryAdd(rootDir.FullName, 0);

        var filesIter = CollectFiles(rootDir, seen, degreeOfParallelism, 0);

        foreach (string x in filesIter)
            yield return x;
    }

    static IEnumerable<string> CollectFiles(DirectoryInfo dir, ConcurrentDictionary<string, byte> seen,
        int degreeOfParallelism, int depth)
    {
        foreach (string f in FilesOf(dir))
            yield return f;

        List<string> files = [];

        var directories = SafeDirs(dir);

        if (directories.Length == 0) yield break;

        // no need to parallelize for 1 directory
        if (directories.Length == 1 || depth > 2)
            SerialScan(directories, seen, degreeOfParallelism, files, depth);
        else
            ParallelScan(directories, seen, degreeOfParallelism, files, depth);

        foreach (string f in files)
            yield return f;
    }

    static void SerialScan(DirectoryInfo[] directories, ConcurrentDictionary<string, byte> seen,
        int degreeOfParallelism, List<string> files, int depth)
    {
        foreach (var child in directories)
        {
            try
            {
                if ((child.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0)
                    continue;

                string physical = string.IsNullOrEmpty(child.LinkTarget)
                    ? child.FullName
                    : child.ResolveLinkTarget(true)?.FullName ?? child.FullName;

                if (!seen.TryAdd(physical, 0))
                    continue;
            }
            catch
            {
                continue;
            }

            files.AddRange(CollectFiles(child, seen, degreeOfParallelism, depth + 1));
        }
    }

    static void ParallelScan(DirectoryInfo[] directories, ConcurrentDictionary<string, byte> seen,
        int degreeOfParallelism, List<string> files, int depth)
    {
        directories.AsParallel()
            .WithDegreeOfParallelism(degreeOfParallelism)
            .ForAll(child =>
            {
                try
                {
                    if ((child.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0)
                        return;

                    string physical = string.IsNullOrEmpty(child.LinkTarget)
                        ? child.FullName
                        : child.ResolveLinkTarget(true)?.FullName ?? child.FullName;

                    if (!seen.TryAdd(physical, 0))
                        return;
                }
                catch
                {
                    return;
                }

                var local = CollectFiles(child, seen, degreeOfParallelism, depth + 1).ToList();

                lock (files) files.AddRange(local);
            });
    }
}
