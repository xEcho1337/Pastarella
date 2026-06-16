using System.Text.RegularExpressions;
using Pastarella.Core.Models;

namespace Pastarella.Core.MacOS.Drivers;

public class DriverKitScanner : IDriverScanner
{
    public IEnumerable<DriverInfo> Scan()
    {
        var results = new List<DriverInfo>();

        PlatformHelpers.TryDo(() =>
        {
            string output = PlatformHelpers.RunProcessAndCaptureOutput(
                "systemextensionsctl", "list", timeoutMs: 2000);

            results.AddRange(ParseSystemExtensions(output)
                .OfType<DriverInfo>());
        });

        return results;
    }

    private static IEnumerable<DriverInfo?> ParseSystemExtensions(string output)
    {
        string[] lines = output.Split('\n');

        if (lines.Length == 0)
            yield break;

        DriverType? currentType = null;

        for (int i = 1; i < lines.Length; i++)
        {
            string raw = lines[i];

            if (string.IsNullOrWhiteSpace(raw))
                continue;

            // section header
            if (raw.TrimStart().StartsWith("--- "))
            {
                currentType = ParseExtensionType(raw);
                // next line is column headers, skip it
                i++;
                continue;
            }

            // skip column header line
            if (raw.TrimStart().StartsWith("enabled"))
                continue;

            if (currentType is null)
                continue;

            // data line
            string trimmed = raw.Trim();
            string[] parts = trimmed.Split('\t', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 4)
                continue;

            string bundleWithVersion;
            string name;
            string state;
            bool loaded;

            switch (parts.Length)
            {
                case 6:
                    bundleWithVersion = parts[3];
                    name = parts[4];
                    state = parts[5].Trim('[', ']');
                    break;
                case 5:
                    bundleWithVersion = parts[2];
                    name = parts[3];
                    state = parts[4].Trim('[', ']');
                    break;
                default:
                    bundleWithVersion = parts[1];
                    name = parts[2];
                    state = parts[3].Trim('[', ']');
                    break;
            }

            // example: io.tailscale.ipn.macsys.network-extension (1.94.2/101.94.2)
            var bundleMatch = Regex.Match(bundleWithVersion, @"^(.+)\s+\((.+)\)$");
            string bundleId = bundleMatch.Success ? bundleMatch.Groups[1].Value.Trim() : bundleWithVersion;
            string? version = bundleMatch.Success ? bundleMatch.Groups[2].Value.Trim() : null;

            loaded = state.Contains("activated") && !state.Contains("waiting");

            yield return new DriverInfo(
                Name: bundleId.Split('.').LastOrDefault() ?? bundleId,
                DisplayName: name,
                Identifier: bundleId,
                Type: currentType.Value,
                ExecutablePath: null,
                Version: version,
                Loaded: loaded,
                Sha256: "N/A",
                Signer: null
            );
        }
    }

    private static DriverType ParseExtensionType(string header)
    {
        // header: "--- com.apple.system_extension.driver_extension (...)"
        var m = Regex.Match(header, @"com\.apple\.system_extension\.(\w+)");
        string type = m.Success ? m.Groups[1].Value : "";

        return type switch
        {
            "cmio" => DriverType.CameraExtension,
            "driver_extension" => DriverType.DriverExtension,
            "network_extension" => DriverType.NetworkExtension,
            _ => DriverType.KernelExtension
        };
    }
}
