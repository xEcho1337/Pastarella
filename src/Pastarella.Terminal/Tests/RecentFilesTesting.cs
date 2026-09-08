using System.Collections.Concurrent;
using System.Diagnostics;

namespace Pastarella.Terminal.Tests;

public class RecentFilesTesting
{
    public static void Main()
    {
        string root = OperatingSystem.IsWindows()
            ? @"C:\Users"
            : "/Users/";

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true
        };

        long files = Benchmark("EnumerateFiles", () =>
        {
            long count = 0;
            foreach (string file in Directory.EnumerateFiles(root, "*", options))
                count++;
            return count;
        });

        Console.WriteLine($"  files: {files:N0}");
        Console.WriteLine();

        long customFiles = Benchmark("CustomEnumerateFiles", () =>
        {
            long count = 0;
            foreach (string file in CustomFilesIterator(root))
                count++;
            return count;
        });

        Console.WriteLine($"  files: {customFiles:N0}");
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

    static readonly EnumerationOptions DefaultOptions = new() { IgnoreInaccessible = true };

    static IEnumerable<string> FilesOf(DirectoryInfo dir)
    {
        foreach (FileInfo file in dir.EnumerateFiles("*", DefaultOptions))
            yield return file.FullName;
    }

    static IEnumerable<string> CollectFiles(DirectoryInfo dir, ConcurrentDictionary<string, byte> seen,
        int degreeOfParallelism, int depth)
    {
        foreach (string f in FilesOf(dir))
            yield return f;

        List<string> files = [];

        var directories = dir.GetDirectories("*", DefaultOptions);

        if (directories.Length == 0) yield break;

        if (depth > 2)
            serialScan(directories, seen, degreeOfParallelism, files, depth);
        else
            parallelScan(directories, seen, degreeOfParallelism, files, depth);

        foreach (string f in files)
            yield return f;
    }

    private static void serialScan(DirectoryInfo[] directories, ConcurrentDictionary<string, byte> seen,
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

    private static void parallelScan(DirectoryInfo[] directories, ConcurrentDictionary<string, byte> seen,
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
