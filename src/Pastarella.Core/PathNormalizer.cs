namespace Pastarella.Core;

public static class PathNormalizer
{
    public static string? Normalize(string path)
    {
        if (path.Length == 0)
            return null;

        if (Context.Os == Context.OS.Windows)
        {
            if (path[0] == '\\')
            {
                // NT paths

                if (path[1..].StartsWith("SystemRoot\\"))
                    return $"{Environment.GetEnvironmentVariable("SystemRoot")}\\{string.Join('\\', path[("\\SystemRoot\\".Length + 1)..])}";

                string[] split = path[1..].Split('\\');
                string ntDevice = $"\\{string.Join('\\', split[0..2])}";

                char? drive = null;
                foreach ((char k, string v) in Windows.ForensicScanners.Storages.DriveMap)
                {
                    if (v == ntDevice)
                    {
                        drive = k;
                        break;
                    }
                }

                if (drive is char letter)
                    return $"{letter}:\\{string.Join('\\', split[2..])}";
                else if (split[1] == "Mup")
                    return $"\\\\{string.Join('\\', split[2..])}";
            }
            else if (path.StartsWith("system32", StringComparison.OrdinalIgnoreCase))
            {
                return $"{Environment.GetEnvironmentVariable("SystemRoot")}\\{string.Join('\\', path[("\\SystemRoot\\".Length + 1)..])}";
            }
        }
        else
        {
            // UNIX paths

            if (path[0] == '~')
                return Environment.GetEnvironmentVariable("HOME") + path[1..];
        }

        return path;
    }
}
