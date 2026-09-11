using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Principal;

namespace Pastarella.Core;

public static class PlatformHelpers
{
    public static readonly ConcurrentDictionary<string, string> HashCache = new();

    private static int _cacheHits;
    public static int CacheHits => Volatile.Read(ref _cacheHits);

    public static bool IsElevated()
    {
        if (OperatingSystem.IsWindows())
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);

            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        return Unix.Native.LibC.getuid() == 0;
    }

    public static T? TryGet<T>(Func<T> getter)
    {
        try
        {
            return getter();
        }
        catch
        {
            return default;
        }
    }

    public static void TryDo(Action action)
    {
        try
        {
            action();
        }
        catch
        {
            // ignored
        }
    }

    public static void TryExecOrDefault<T>(Func<T> func, Action<T> action, Action defaultAction)
    {
        try
        {
            var res = func();
            action(res);
        }
        catch
        {
            defaultAction();
        }
    }

    public static void TryExecNotNull<T>(Func<T> func, Action<T> action)
    {
        TryExecOrDefault(func, action, () => { });
    }

    public static string RunProcessAndCaptureOutput(string fileName, string args, int timeoutMs = 5000)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var proc = Process.Start(psi)!;
        string outp = proc.StandardOutput.ReadToEnd();
        string err = proc.StandardError.ReadToEnd();

        if (!proc.WaitForExit(timeoutMs))
            TryDo(proc.Kill);

        if (!string.IsNullOrWhiteSpace(err))
            outp += '\n' + err;

        return outp;
    }

    public static string? GetSha256(string? _filePath)
    {
        if (_filePath is not string filePath)
            return null;

        try
        {
            if (HashCache.TryGetValue(filePath, out string? sha256))
            {
                Interlocked.Increment(ref _cacheHits);
                return sha256;
            }

            byte[] data = File.ReadAllBytes(filePath);

            Span<byte> hash = stackalloc byte[32];
            SHA256.HashData(data, hash);

            string hashHex = Convert.ToHexString(hash).ToLowerInvariant();
            HashCache[filePath] = hashHex;

            return hashHex;
        }
        catch
        {
            return null;
        }
    }

    public static string? FindExecutableInPath(string exec)
    {
        if (Path.IsPathFullyQualified(exec))
            return exec;

        string? pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (pathEnv == null)
            return null;

        foreach (string path in pathEnv.Split(':'))
        {
            foreach (string execFilePath in Directory.GetFiles(path))
            {
                if (Path.GetFileName(execFilePath) == exec)
                    return execFilePath;
            }
        }

        return null;
    }
}
