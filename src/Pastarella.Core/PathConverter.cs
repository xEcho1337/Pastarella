using System.Text;

namespace Pastarella.Core;

public static class PathConverter
{
    public static string Normalize(string path)
    {
        if (path.Length == 0)
            return path;

        if (Context.Os == Context.OS.Windows)
        {
            if (path[0] == '\\')
            {
                // NT paths

                if (path[1..].StartsWith("??\\"))
                    return path[4..];

                if (path[1..].StartsWith("SystemRoot\\"))
                    return $"{Environment.GetEnvironmentVariable("SystemRoot")}\\{string.Join('\\', path["\\SystemRoot\\".Length..])}";

                string[] split = path[1..].Split('\\');
                string ntDevice = $"\\{string.Join('\\', split[0..2])}";

                char? drive = null;
                foreach ((char k, string v) in Windows.StorageScanner.DriveMap)
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
                return $"{Environment.GetEnvironmentVariable("SystemRoot")}\\System32\\{string.Join('\\', path[("SystemRoot".Length - 1)..])}";
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

    public static string[] Unescape(string path)
    {
        var parts = new List<string>();
        var builder = new StringBuilder(path.Length / 2);

        int i = 0;
        while (i < path.Length)
        {
            switch (path[i])
            {
                case '"':
                    {
                        int start = i + 1;
                        int lastQuote;
                        do
                        {
                            lastQuote = path[start..].IndexOf('"');
                            if (lastQuote == -1)
                            {
                                builder.Append(path[start..]);
                                builder.Clear();

                                return [.. parts];
                            }

                            if (path[lastQuote - 1] == '\\')
                            {
                                builder.Append(path[start..(lastQuote - 1)]);
                                start = lastQuote + 1;
                                continue;
                            }

                            i = lastQuote + 1;

                            if ((lastQuote + 1) < path.Length && path[lastQuote + 1] == ' ')
                            {
                                builder.Append(path[start..lastQuote]);
                                goto outer;
                            }

                            builder.Append(path[start..(lastQuote + 1)]);

                            parts.Add(builder.ToString());
                            builder.Clear();

                            goto outer;
                        } while (true);
                    }
                case ' ':
                    parts.Add(builder.ToString());
                    builder.Clear();
                    break;
                case '\\':
                    if (path[i + 1] == ' ')
                        i++;
                    goto default;
                default:
                    builder.Append(path[i]);
                    break;
            }
        outer:
            i++;
        }

        parts.Add(builder.ToString());
        builder.Clear();

        return [.. parts];
    }
}
