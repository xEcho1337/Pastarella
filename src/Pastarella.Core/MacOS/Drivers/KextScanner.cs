using System.Text.RegularExpressions;
using Claunia.PropertyList;
using Pastarella.Core.Models;

namespace Pastarella.Core.MacOS.Drivers;

public class KextScanner : IDriverScanner
{
    private const string SystemExtensionsPath = "/System/Library/Extensions";
    private const string LibraryExtensionsPath = "/Library/Extensions";

    public IEnumerable<DriverInfo> Scan()
    {
        var loadedIds = GetLoadedKextIdentifiers();

        return EnumerateKextDirectories()
            .Select(kextDir => ParseKext(kextDir, loadedIds))
            .OfType<DriverInfo>()
            .ToList();
    }

    private static DriverInfo? ParseKext(string kextDir, HashSet<string> loadedIds)
    {
        try
        {
            string plistPath = Path.Combine(kextDir, "Contents", "Info.plist");
            if (!File.Exists(plistPath))
                return null;

            var plist = (NSDictionary) PropertyListParser.Parse(plistPath);

            string name = Path.GetFileNameWithoutExtension(kextDir);
            string identifier = GetPlistString(plist, "CFBundleIdentifier")
                ?? name;

            string displayName = GetPlistString(plist, "CFBundleDisplayName")
                ?? GetPlistString(plist, "CFBundleName")
                ?? name;

            string? executableName = GetPlistString(plist, "CFBundleExecutable");
            string? execPath = executableName is not null
                ? Path.Combine(kextDir, "Contents", "MacOS", executableName) : null;

            string? version = GetPlistString(plist, "CFBundleShortVersionString")
                ?? GetPlistString(plist, "CFBundleVersion");

            string path = plistPath;

            if (File.Exists(execPath))
                path = execPath;

            string hash = PlatformHelpers.GetSha256(path);
            bool loaded = loadedIds.Contains(identifier) || loadedIds.Contains(name);

            (string? teamId, string? signer) = GetSignatureInfo(execPath);

            return new DriverInfo(
                Name: name,
                DisplayName: displayName,
                Identifier: identifier,
                Type: DriverType.Kext,
                ExecutablePath: execPath,
                Version: version,
                Loaded: loaded,
                Sha256: hash,
                Signer: signer
            );
        }
        catch
        {
            return null;
        }
    }

    private static HashSet<string> GetLoadedKextIdentifiers()
    {
        var loaded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        PlatformHelpers.TryDo(() =>
        {
            string output = PlatformHelpers.RunProcessAndCaptureOutput(
                "kmutil",
                "showloaded --list-only",
                timeoutMs: 8000);

            using var sr = new StringReader(output);
            while (sr.ReadLine() is { } rawLine)
            {
                string line = rawLine.Trim();
                if (line.Length == 0)
                    continue;

                // skip headers
                if (!char.IsDigit(line[0]))
                    continue;

                string[] cols = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (cols.Length < 6)
                    continue;
                string identifier = cols[5];
                loaded.Add(identifier);
            }
        });

        return loaded;
    }

    private static IEnumerable<string> EnumerateKextDirectories()
    {
        foreach (string basePath in new[] { SystemExtensionsPath, LibraryExtensionsPath })
        {
            if (!Directory.Exists(basePath))
                continue;

            foreach (string dir in Directory.EnumerateDirectories(basePath, "*.kext"))
                yield return dir;
        }
    }

    private static (string? TeamId, string? Signer) GetSignatureInfo(string? binaryPath)
    {
        if (string.IsNullOrWhiteSpace(binaryPath) || !File.Exists(binaryPath))
            return (null, null);

        try
        {
            string output = PlatformHelpers.RunProcessAndCaptureOutput(
                "codesign", $"-dv \"{binaryPath}\" 2>&1", timeoutMs: 5000);

            string? teamId = null;
            string? authority = null;

            using var sr = new StringReader(output);
            while (sr.ReadLine() is { } line)
            {
                if (line.StartsWith("TeamIdentifier=", StringComparison.Ordinal))
                    teamId = line["TeamIdentifier=".Length..].Trim();
                else if (authority is null && line.StartsWith("Authority=", StringComparison.Ordinal))
                    authority = line["Authority=".Length..].Trim();
            }

            return (teamId, authority);
        }
        catch
        {
            return (null, null);
        }
    }

    private static string? GetPlistString(NSDictionary dict, string key)
    {
        return !dict.TryGetValue(key, out NSObject? value) || value is not NSString s
            ? null : s.Content;
    }
}
