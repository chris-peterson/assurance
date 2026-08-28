using System;
using Assurance.Compare;
using Assurance.Logging;
using Spiffy.Monitoring;

namespace Assurance;

public class RunResult<T>
{
    readonly ILogRun<T> _logRun;

    internal RunResult(T existing, T replacement, IComparisonStrategy<T> comparisonStrategy, ILogRun<T> logRun)
    {
        Existing = existing;
        Replacement = replacement;
        _logRun = logRun;
        ResultComparison = comparisonStrategy.Compare(existing, replacement);
    }

    public T Existing { get; }
    public T Replacement { get; }

    public ResultComparison ResultComparison { get; }

    [Obsolete("Use ResultComparison.AreEqual instead.")]
    public bool SameResult => ResultComparison.AreEqual;

    public T UseExisting()
    {
        LogUse("existing");
        return Existing;
    }

    public T UseReplacement()
    {
        LogUse("replacement");
        return Replacement;
    }

    public EventContext EventContext => _logRun.EventContext;

    void LogUse(string use)
    {
        _logRun.Log("Use", use);
        _logRun.Complete();
    }

    ~RunResult()
    {
        try
        {
            if (!_logRun.WasCompleted)
            {
                _logRun.Warn("Call UseExisting or UseReplacement in order to avoid this warning");
                LogUse("unknown");
            }
        }
        catch
        {
            // ILogRun is caller-supplied; letting it throw on the finalizer thread would
            // take down the host process, and there is nowhere left to report it.
        }
    }
}
