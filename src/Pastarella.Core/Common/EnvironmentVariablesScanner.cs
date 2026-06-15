using System.Collections;

namespace Pastarella.Core.Common;

public static class EnvironmentVariablesScanner
{
    public static Dictionary<string, string> GetEnvs()
    {
        Dictionary<string, string> dict = [];

        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
            dict[entry.Key.ToString()!] = entry.Value?.ToString() ?? "";

        return dict;
    }
}
