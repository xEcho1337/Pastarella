using Pastarella.Core.Common;
using Pastarella.Core.Models;

namespace Pastarella.Core.Windows;

public class AnalysisDispatcher : IAnalysisDispatcher
{
    public AnalysisReport Report { get; } = new();

    public void AddDispatchers(Dictionary<string, Action> outActions)
    { }
}
