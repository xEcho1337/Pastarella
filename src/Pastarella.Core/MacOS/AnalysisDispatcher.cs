using Pastarella.Core.Common;
using Pastarella.Core.Models;

namespace Pastarella.Core.MacOS;

public class AnalysisDispatcher : IAnalysisDispatcher
{
    public AnalysisReport Report { get; } = new();

    private IPersistenceScanner _persistence = new PersistenceScanner();

    public void AddDispatchers(Dictionary<string, Action> outActions)
    {
        outActions.Remove("Persistence Checks");
        outActions["Persistence Checks (needs sudo)"] = () => Report.Persistences = _persistence.Scan().ToList();
    }
}
