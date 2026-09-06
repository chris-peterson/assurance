using System.Collections.Generic;
using System.Threading;
using Assurance.Logging;
using Spiffy.Monitoring;

namespace Assurance.UnitTests.TestModels;

/// <summary>
/// Hands out runs that count how often a result's finalizer reaches back into them.
/// </summary>
internal class FinalizerAwareLogStrategy : ILogStrategy<List<string>>
{
    public FinalizerAwareLogRun LastRun { get; private set; }

    public ILogRun<List<string>> Begin(string taskName)
    {
        LastRun = new FinalizerAwareLogRun(taskName);
        return LastRun;
    }
}

/// <summary>
/// Counts the reads of <see cref="WasCompleted"/>, which only a result's finalizer performs. A
/// read after the run was closed out means the finalizer ran with nothing left to do, on a
/// thread the caller's log never agreed to be called from.
/// </summary>
internal class FinalizerAwareLogRun : ILogRun<List<string>>
{
    int _completed;
    int _completedChecks;

    public FinalizerAwareLogRun(string taskName)
    {
        EventContext = new EventContext("Assurance", taskName);
    }

    public EventContext EventContext { get; }

    public int CompletedChecks => Volatile.Read(ref _completedChecks);

    public void Log(string field, object value) => EventContext[field] = value;

    public void AppendToValue(string field, string value) => EventContext.AppendToValue(field, value, ",");

    public void LogRunResult(RunResult<List<string>> result) =>
        Log("Result", result.ResultComparison.AreEqual ? "equal" : "notEqual");

    public void Warn(string message) => EventContext.SetToWarning(message);

    public bool WasCompleted
    {
        get
        {
            Interlocked.Increment(ref _completedChecks);
            return Volatile.Read(ref _completed) == 1;
        }
    }

    public void Complete()
    {
        if (Interlocked.Exchange(ref _completed, 1) == 1)
        {
            return;
        }
        EventContext.Dispose();
    }
}
