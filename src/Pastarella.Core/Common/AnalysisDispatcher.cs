using Pastarella.Core.Models;

namespace Pastarella.Core.Common;

public interface IAnalysisDispatcher
{
    AnalysisReport Report { get; }

    void AddDispatchers(Dictionary<string, Action> outActions);
}
