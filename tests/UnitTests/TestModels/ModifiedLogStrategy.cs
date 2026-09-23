using System.Collections.Generic;
using System.Linq;
using Assurance.Logging;
using Spiffy.Monitoring;

namespace Assurance.UnitTests.TestModels;

/// <summary>
/// A log that summarizes the two results instead of listing every difference, which is the route
/// the README documents for a consumer with large object graphs.
/// </summary>
internal class ModifiedLogStrategy : DefaultLogStrategy<List<string>>
{
    public ModifiedLogStrategy(EventContext eventContext = null) : base(eventContext)
    {
    }

    public override ILogRun<List<string>> Begin(string taskName)
    {
        return new ModifiedLogRun(ProvidedEventContext, taskName);
    }
}

internal class ModifiedLogRun : DefaultLogRun<List<string>>
{
    const int MaxLoggedValues = 5;

    public ModifiedLogRun(EventContext eventContext, string taskName) : base(eventContext, taskName)
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
