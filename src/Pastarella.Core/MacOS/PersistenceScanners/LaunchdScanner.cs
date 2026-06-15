using Claunia.PropertyList;
using Pastarella.Core.Models;

namespace Pastarella.Core.MacOS.PersistenceScanners;

public sealed class LaunchdScanner : IPersistenceScanner
{
    private static readonly string[] LaunchdPaths =
    [
        "/System/Library/LaunchDaemons",
        "/System/Library/LaunchAgents",
        "/Library/LaunchDaemons",
        "/Library/LaunchAgents",
        PlatformHelpers.NormalizePath("~/Library/LaunchAgents")
    ];

    public IEnumerable<PersistenceEntry> Scan()
    {
        var entries = new List<PersistenceEntry>();

        foreach (string path in LaunchdPaths)
        {
            if (!Directory.Exists(path))
                continue;

            entries.AddRange(ReadLaunchdDirectory(path));
        }

        return entries;
    }

    private static IEnumerable<PersistenceEntry> ReadLaunchdDirectory(string path)
    {
        bool isDaemon = path.EndsWith("LaunchDaemons", StringComparison.OrdinalIgnoreCase);

        foreach (string fileName in Directory.EnumerateFiles(path))
        {
            var entry = ParseLaunchdEntry(fileName, isDaemon);

            if (entry != null)
                yield return entry;
        }
    }

    private static PersistenceEntry? ParseLaunchdEntry(string plistPath, bool isDaemon)
    {
        try
        {
            var root = PropertyListParser.Parse(plistPath);

            if (root is not NSDictionary dict)
                return null;

            string? label = GetString(dict, "Label");
            string? executable = GetString(dict, "Program");

            if (string.IsNullOrWhiteSpace(executable))
            {
                var arguments = GetStringArray(dict, "ProgramArguments");
                executable = arguments.FirstOrDefault();
            }

            if (string.IsNullOrWhiteSpace(executable))
                return null;

            bool? runAtLoad = GetBool(dict, "RunAtLoad");
            bool? keepAlive = GetBool(dict, "KeepAlive");

            return new PersistenceEntry.ScheduledTask
            {
                Name = label ?? Path.GetFileNameWithoutExtension(plistPath),
                Path = plistPath,
                Action = new ExecScheduledAction
                {
                    Path = executable,
                    Sha256 = PlatformHelpers.GetSha256(executable),
                },

                Type = PersistenceType.Launchd,

                Trigger = isDaemon
                    ? ExecutionTrigger.Boot
                    : ExecutionTrigger.UserLogin,

                Privilege = isDaemon
                    ? PersistencePrivilege.Admin
                    : PersistencePrivilege.User,

                Metadata =
                {
                    ["RunAtLoad"] = runAtLoad ?? false,
                    ["KeepAlive"] = keepAlive ?? false
                },

                RiskScore = 0, // this is calculated later
                Schedule = runAtLoad ?? false ? "Startup" : "N/A",
            };
        }
        catch
        {
            return null;
        }
    }

    private static string? GetString(NSDictionary dict, string key)
    {
        return !dict.TryGetValue(key, out var value)
            ? null
            : (value as NSString)?.Content;
    }

    private static bool? GetBool(NSDictionary dict, string key)
    {
        return !dict.TryGetValue(key, out var value)
            ? null
            : (value as NSNumber)?.ToBool();
    }

    private static List<string> GetStringArray(NSDictionary dict, string key)
    {
        if (!dict.ContainsKey(key) || dict[key] is not NSArray array)
            return [];

        return array
            .Select(x => x.ToString() ?? string.Empty)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }
}
