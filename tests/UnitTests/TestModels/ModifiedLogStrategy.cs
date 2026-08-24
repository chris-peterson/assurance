using System.Collections.Generic;
using System.Linq;
using Assurance.Logging;
using Spiffy.Monitoring;

namespace Assurance.UnitTests.TestModels;

internal class ModifiedLogStrategy : DefaultLogStrategy<List<string>>
{
    const int MaxLoggedValues = 5;

    public ModifiedLogStrategy(EventContext eventContext = null) : base(eventContext)
    {
    }

    public override void LogRunResult(RunResult<List<string>> result)
    {
        if (result.ResultComparison.AreEqual)
        {
            Log("Result", "equal");
            return;
        }
        Log("Result", "notEqual");
        Log("ExistingFive", Summarize(result.Existing));
        Log("ReplacementFive", Summarize(result.Replacement));
    }

    static string Summarize(List<string> values)
    {
        // an implementation that was undefined or threw yields a null result
        return values == null ? "" : string.Join(",", values.Take(MaxLoggedValues));
    }
}
