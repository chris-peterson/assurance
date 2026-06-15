using Assurance.Logging;
using Spiffy.Monitoring;
using System.Collections.Generic;
using System.Linq;

namespace Assurance.UnitTests.TestModels;

internal class ModifiedLogStrategy : DefaultLogStrategy<List<string>>
{
    public ModifiedLogStrategy(string taskName, EventContext eventContext = null) : base(taskName, eventContext)
    {
    }

    public string CustomExistingDifferenceField { get; } = "ExistingFive";
    public string CustomReplacementDifferenceField { get; } = "ReplacementFive";
    public override void LogRunResult(RunResult<List<string>> result)
    {
        if (result.ResultComparison.AreEqual)
        {
            Log("Result", "equal");
        }
        else
        {
            Log("Result", "notEqual");
            Log("ExistingFive", string.Join(',', result.Existing.Take(5)));
            Log("ReplacementFive", string.Join(',', result.Replacement.Take(5)));
        }
    }
}