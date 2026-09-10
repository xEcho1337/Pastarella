namespace Pastarella.Core;

public static class PathNormalizer
{
    public static string? Normalize(string path) {
        if (path.Length == 0)
            return null;

        if (Context.Os == Context.OS.Windows)
        {
            if (path.StartsWith(@"\SystemRoot"))
                return Environment.GetEnvironmentVariable("SystemRoot") + path[@"\SystemRoot".Length..];

            if (path.StartsWith("System32", StringComparison.OrdinalIgnoreCase))
                return Environment.GetEnvironmentVariable("SystemRoot") + '\\' + path;

            if (path[0] == '\\')
            {
                if (path.StartsWith("??\\"))
                    return path[3..];
                else if (path.Length > 1 && path[1] != '\\')
                    throw new NotImplementedException($"Use RtlNtPathNameToDosPathName or family.\nPath: {path}");
                else
                    return path; // paths that starts with '\\' are network drives
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
