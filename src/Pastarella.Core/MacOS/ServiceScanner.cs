using System.Text.RegularExpressions;
using Claunia.PropertyList;
using Pastarella.Core.Models;

namespace Pastarella.Core.MacOS;

public class ServiceScanner : IServiceScanner
{
    private static readonly string[] LaunchdPaths =
    [
        "/System/Library/LaunchDaemons",
        "/System/Library/LaunchAgents",
        "/Library/LaunchDaemons",
        "/Library/LaunchAgents",
        PlatformHelpers.NormalizePath("~/Library/LaunchAgents")
    ];

    public IEnumerable<ServiceInfo> Scan()
    {
        var results = new List<ServiceInfo>();

        Dictionary<string, LaunchctlState> runtimeStates;

        try
        {
            runtimeStates = ReadRuntimeStates();
        }
        catch
        {
            runtimeStates = new Dictionary<string, LaunchctlState>(StringComparer.OrdinalIgnoreCase);
        }

        var seenLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string dir in LaunchdPaths)
        {
            if (!Directory.Exists(dir))
                continue;

            foreach (string plistPath in Directory.EnumerateFiles(dir, "*.plist", SearchOption.TopDirectoryOnly))
            {
                if (!TryParseLaunchdPlist(plistPath, runtimeStates, out var serviceInfo, out string? label))
                    continue;

                if (!string.IsNullOrWhiteSpace(label))
                    seenLabels.Add(label);

                if (serviceInfo != null)
                    results.Add(serviceInfo);
            }
        }

        foreach (var kvp in runtimeStates)
        {
            if (seenLabels.Contains(kvp.Key))
                continue;

            results.Add(CreateRuntimeOnlyService(kvp.Key, kvp.Value));
        }

        return results;
    }

    private static Dictionary<string, LaunchctlState> ReadRuntimeStates()
    {
        var map = new Dictionary<string, LaunchctlState>(StringComparer.OrdinalIgnoreCase);

        string output = PlatformHelpers.RunProcessAndCaptureOutput("launchctl", "list");
        using var sr = new StringReader(output);

        bool firstLine = true;

        while (sr.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (firstLine)
            {
                firstLine = false;

                if (line.Contains("Label", StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            string[] parts = Regex.Split(line.Trim(), @"\s+");
            if (parts.Length < 3)
                continue;

            int? pid = int.TryParse(parts[0], out int parsedPid) ? parsedPid : null;
            int? lastExitStatus = int.TryParse(parts[1], out int parsedExit) ? parsedExit : null;
            string label = parts[2];

            map[label] = new LaunchctlState(pid, lastExitStatus);
        }

        return map;
    }

    private static bool TryParseLaunchdPlist(
        string plistPath,
        IReadOnlyDictionary<string, LaunchctlState> runtimeStates,
        out ServiceInfo? serviceInfo, out string? label)
    {
        serviceInfo = null;
        label = null;

        try
        {
            var root = PropertyListParser.Parse(plistPath);
            if (root is not NSDictionary dict)
                return false;

            label = GetString(dict, "Label");
            string displayName = GetString(dict, "DisplayName")
                                 ?? label
                                 ?? Path.GetFileNameWithoutExtension(plistPath);

            string? executablePath = GetString(dict, "Program");

            if (string.IsNullOrWhiteSpace(executablePath))
            {
                var args = GetStringArray(dict, "ProgramArguments");
                executablePath = args.FirstOrDefault();
            }

            if (string.IsNullOrWhiteSpace(executablePath))
                executablePath = GetString(dict, "BundleProgram");

            if (string.IsNullOrWhiteSpace(executablePath))
                executablePath = plistPath;

            string serviceName = label ?? Path.GetFileNameWithoutExtension(plistPath);

            var status = ServiceStatus.Stopped;
            if (!string.IsNullOrWhiteSpace(label) && runtimeStates.TryGetValue(label, out LaunchctlState runtime))
                status = runtime.Pid is > 0 ? ServiceStatus.Running : ServiceStatus.Stopped;

            string shaSource = File.Exists(executablePath)
                ? executablePath
                : serviceName;

            serviceInfo = new ServiceInfo(
                status,
                ServiceType.MacOSService,
                serviceName,
                displayName,
                executablePath, [],
                PlatformHelpers.GetSha256(shaSource)
            );

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static ServiceInfo CreateRuntimeOnlyService(string label, LaunchctlState state)
    {
        string executablePath = GetLaunchctlProgramPath(label) ?? "N/A";

        return new ServiceInfo(
            state.Pid is > 0 ? ServiceStatus.Running : ServiceStatus.Stopped,
            ServiceType.MacOSService,
            label,
            label,
            executablePath, [],
            PlatformHelpers.GetSha256(executablePath)
        );
    }

    private static string? GetLaunchctlProgramPath(string label)
    {
        try
        {
            string output = PlatformHelpers.RunProcessAndCaptureOutput("launchctl", $"print {label}");

            var match = Regex.Match(output, @"program\s*=\s*""([^""]+)""");
            if (match.Success)
            {
                string path = match.Groups[1].Value;
                if (File.Exists(path))
                    return path;
            }

            match = Regex.Match(output, @"path\s*=\s*""([^""]+)""");
            if (match.Success)
            {
                string path = match.Groups[1].Value;
                if (File.Exists(path))
                    return path;
            }
        }
        catch
        {
            // ignored
        }

        return null;
    }

    private static string? GetString(NSDictionary dict, string key)
    {
        if (!dict.TryGetValue(key, out var value) || value == null)
            return null;

        return value switch
        {
            NSString s => s.Content,
            NSNumber n => n.ToString(),
            _ => value.ToString()
        };
    }

    private static bool? GetBool(NSDictionary dict, string key)
    {
        if (!dict.TryGetValue(key, out var value) || value == null)
            return null;

        return value switch
        {
            NSNumber n => n.ToBool(),
            NSString s when bool.TryParse(s.Content, out bool b) => b,
            _ => null
        };
    }

    private static List<string> GetStringArray(NSDictionary dict, string key)
    {
        if (!dict.TryGetValue(key, out var value) || value is not NSArray array)
            return [];

        var result = new List<string>(array.Count);

        foreach (var item in array)
        {
            string? s = item switch
            {
                NSString str => str.Content,
                NSNumber num => num.ToString(),
                _ => item?.ToString()
            };

            if (!string.IsNullOrWhiteSpace(s))
                result.Add(s);
        }

        return result;
    }

    private readonly record struct LaunchctlState(int? Pid, int? LastExitStatus);
}
