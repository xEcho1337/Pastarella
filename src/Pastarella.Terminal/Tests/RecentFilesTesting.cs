using System.Collections.Concurrent;
using System.Diagnostics;

namespace Pastarella.Terminal.Tests;

public class RecentFilesTesting
{
    public static void Main()
    {
        string root = OperatingSystem.IsWindows()
            ? @"C:\Users"
            : "/Users";

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true
        };

        long files = Benchmark("EnumerateFiles", () =>
        {
            long count = 0;
            foreach (string unused in Directory.EnumerateFiles(root, "*", options))
                count++;
            return count;
        });

        Console.WriteLine($"  files: {files:N0}");
        Console.WriteLine();

        long customFiles = Benchmark("CustomEnumerateFiles", () =>
        {
            long count = 0;
            foreach (string unused in CustomFilesIterator(root))
                count++;
            return count;
        });

        Console.WriteLine($"  files: {customFiles:N0}");
        Console.WriteLine();

        long fastFiles = Benchmark("FastFiles", () =>
        {
            long count = 0;
            foreach (string unused in FastFilesIterator(root))
                count++;
            return count;
        });

        Console.WriteLine($"  files: {fastFiles:N0}");
        Console.WriteLine();
    }

    static T Benchmark<T>(string name, Func<T> func)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var sw = Stopwatch.StartNew();
        T result = func();
        sw.Stop();

        Console.WriteLine($"{name,-35} {sw.Elapsed.TotalSeconds:F3}s");
        return result;
    }

    static IEnumerable<string> FastFilesIterator(string root)
    {
        int degreeOfParallelism = Math.Clamp(Environment.ProcessorCount, 1, 8);
        var seen = new ConcurrentDictionary<string, byte>();
        var dirs = new ConcurrentQueue<string>();
        var files = new ConcurrentBag<string>();

        seen.TryAdd(new DirectoryInfo(root).FullName, 0);
        dirs.Enqueue(root);

        int pending = 1;

        Task[] workers = new Task[degreeOfParallelism];
        for (int i = 0; i < degreeOfParallelism; i++)
        {
            workers[i] = Task.Run(() =>
            {
                while (true)
                {
                    if (!dirs.TryDequeue(out string? current))
                    {
                        if (Volatile.Read(ref pending) == 0)
                            return;

                        Thread.Yield();
                        continue;
                    }

                    ProcessDir(current, dirs, files, seen, ref pending);

                    if (Interlocked.Decrement(ref pending) == 0)
                        return;
                }
            });
        }

        Task.WaitAll(workers);

        return files;
    }

    static void ProcessDir(string path, ConcurrentQueue<string> dirs, ConcurrentBag<string> files,
        ConcurrentDictionary<string, byte> seen, ref int pending)
    {
        FileSystemInfo[] entries;
        try
        {
            entries = new DirectoryInfo(path).GetFileSystemInfos("*", DefaultOptions);
        }
        catch
        {
            return;
        }

        foreach (FileSystemInfo entry in entries)
        {
            if (entry is FileInfo file)
            {
                files.Add(file.FullName);
                continue;
            }

            var dir = (DirectoryInfo)entry;

            try
            {
                if ((dir.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0)
                    continue;

                if (!string.IsNullOrEmpty(dir.LinkTarget))
                {
                    string physical = dir.ResolveLinkTarget(true)?.FullName ?? dir.FullName;

                    if (!seen.TryAdd(physical, 0))
                        continue;
                }
            }
            catch
            {
                continue;
            }

            dirs.Enqueue(dir.FullName);
            Interlocked.Increment(ref pending);
        }
    }

    static readonly EnumerationOptions DefaultOptions = new() { IgnoreInaccessible = true };

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

    // Echo's Version
    static IEnumerable<string> FilesOf(DirectoryInfo dir)
    {
        foreach (string f in SafeFiles(dir))
            yield return f;
    }

    static IEnumerable<string> CustomFilesIterator(string root)
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
