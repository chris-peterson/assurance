using Assurance.Logging;
using Spiffy.Monitoring;

namespace Assurance.UnitTests.TestModels;

/// <summary>
/// A log that never provides an <see cref="EventContext"/>, which the runner
/// needs in order to record per-implementation timings.
/// </summary>
internal class NullContextLogStrategy : ILogStrategy<string>
{
    public ILogRun<string> Begin(string taskName)
    {
        return new NullContextLogRun();
    }
}

internal class NullContextLogRun : ILogRun<string>
{
    public EventContext EventContext => null;

    public bool WasCompleted { get; private set; }

    public void AppendToValue(string field, string value)
    {
    }

    public void Log(string field, object value)
    {
    }

    public void LogRunResult(RunResult<string> result)
    {
    }

    public void Warn(string message)
    {
    }

    public void Complete()
    {
        WasCompleted = true;
    }
}
