using Pastarella.Core.Models;

namespace Pastarella.Core.MacOS.PersistenceScanners;

public sealed class CronScanner : IPersistenceScanner
{
    private static readonly string[] CronPaths =
    [
        "/etc/crontab",
        "/etc/cron.d",
        "/usr/lib/cron/tabs"
    ];

    public IEnumerable<PersistenceEntry> Scan()
    {
        var entries = new List<PersistenceEntry>();

        foreach (string path in CronPaths)
        {
            if (path == "/usr/lib/cron/tabs" && !PlatformHelpers.IsElevated())
                continue;

            if (File.Exists(path))
            {
                entries.AddRange(ReadCronFile(path));
                continue;
            }

            if (Directory.Exists(path))
                entries.AddRange(ReadCronDirectory(path));
        }

        return entries;
    }

    private static IEnumerable<PersistenceEntry> ReadCronDirectory(string path)
    {
        foreach (string filePath in Directory.EnumerateFiles(path))
        {
            foreach (var entry in ReadCronFile(filePath))
                yield return entry;
        }
    }

    private static IEnumerable<PersistenceEntry> ReadCronFile(string filePath)
    {
        int lineNumber = 0;

        foreach (string line in File.ReadLines(filePath))
        {
            lineNumber++;

            if (TryParseEntry(filePath, line, lineNumber, out var entry))
                yield return entry;
        }
    }

    private static bool TryParseEntry(string sourcePath, string line, int lineNumber, out PersistenceEntry entry)
    {
        entry = null!;

        string trimmed = line.Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
            return false;

        if (trimmed.StartsWith('#'))
            return false;

        if (LooksLikeEnvironmentAssignment(trimmed))
            return false;

        var tokens = Tokenize(trimmed);

        if (tokens.Count == 0)
            return false;

        bool isUserCrontab = sourcePath.StartsWith("/usr/lib/cron/tabs", StringComparison.Ordinal);

        string schedule;
        string user;
        string commandLine;

        if (tokens[0].StartsWith('@'))
        {
            if (tokens.Count < 2)
                return false;

            schedule = tokens[0];
            commandLine = trimmed[tokens[0].Length..].TrimStart();
            user = isUserCrontab ? Path.GetFileName(sourcePath) : "root";
        }
        else if (isUserCrontab)
        {
            if (tokens.Count < 6)
                return false;

            schedule = string.Join(' ', tokens.Take(5));
            commandLine = string.Join(' ', tokens.Skip(5));
            user = Path.GetFileName(sourcePath);
        }
        else if (tokens.Count < 7)
        {
            // comment here just to remove the IDE error
            return false;
        }
        else
        {
            schedule = string.Join(' ', tokens.Take(5));
            user = tokens[5];
            commandLine = string.Join(' ', tokens.Skip(6));
        }

        if (string.IsNullOrWhiteSpace(commandLine))
            return false;

        string filePath = ExtractExecutableToken(commandLine);
        entry = new PersistenceEntry.ScheduledTask
        {
            Name = $"{Path.GetFileName(sourcePath)}:{lineNumber}",
            Path = sourcePath,

            Action = new ExecScheduledAction
            {
                Path = filePath,
                Sha256 = PlatformHelpers.GetSha256(filePath),
            },

            Trigger = schedule.StartsWith("@reboot", StringComparison.OrdinalIgnoreCase)
                ? ExecutionTrigger.Boot
                : ExecutionTrigger.Scheduled,

            Privilege = user.Equals("root", StringComparison.OrdinalIgnoreCase)
                ? PersistencePrivilege.Admin
                : PersistencePrivilege.User,

            Type = PersistenceType.Cron,
            Schedule = schedule,

            Metadata = new Dictionary<string, object>
            {
                ["LineNumber"] = lineNumber,
                ["SourceLine"] = trimmed,
                ["Schedule"] = schedule,
                ["CommandLine"] = commandLine,
                ["User"] = user
            }
        };

        return true;
    }

    private static bool LooksLikeEnvironmentAssignment(string line)
    {
        int eq = line.IndexOf('=');
        if (eq <= 0)
            return false;

        string left = line[..eq].Trim();

        if (string.IsNullOrWhiteSpace(left))
            return false;

        if (left.Any(c => !(char.IsLetterOrDigit(c) || c == '_')))
            return false;

        return !left.StartsWith('@');
    }

    private static List<string> Tokenize(string line)
    {
        return line
            .Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }

    private static string ExtractExecutableToken(string commandLine)
    {
        foreach (string token in Tokenize(commandLine))
        {
            if (!LooksLikeEnvironmentAssignment(token))
                return token;
        }

        return commandLine;
    }
}
