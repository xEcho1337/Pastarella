using Pastarella.Core.Common;
using Pastarella.Core.Models;

namespace Pastarella.Core.Linux;

public class AnalysisDispatcher : IAnalysisDispatcher
{
    public static bool UsrMerged { get; private set; }
    public static string ModulesPath { get; private set; } = "/lib/modules";

    public AnalysisReport Report { get; } = new();

    public AnalysisDispatcher()
    {
        if (new DirectoryInfo("/lib").LinkTarget is string target)
            UsrMerged = target == "usr/lib";

        if (UsrMerged)
            ModulesPath = "/usr/lib/modules";
    }

    public void AddDispatchers(Dictionary<string, Action> outActions)
    { }
}
