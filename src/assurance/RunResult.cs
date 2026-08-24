using System;
using Assurance.Compare;
using Assurance.Logging;
using Spiffy.Monitoring;

namespace Assurance;

public class RunResult<T>
{
    readonly ILogStrategy<T> _logStrategy;

    internal RunResult(T existing, T replacement, IComparisonStrategy<T> comparisonStrategy, ILogStrategy<T> logStrategy)
    {
        Existing = existing;
        Replacement = replacement;
        _logStrategy = logStrategy;
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

    public EventContext EventContext => _logStrategy.EventContext;

    void LogUse(string use)
    {
        _logStrategy.Log("Use", use);
        _logStrategy.Complete();
    }

    ~RunResult()
    {
        try
        {
            if (!_logStrategy.WasCompleted)
            {
                _logStrategy.Warn("Call UseExisting or UseReplacement in order to avoid this warning");
                LogUse("unknown");
            }
        }
        catch
        {
            // ILogStrategy is caller-supplied; letting it throw on the finalizer thread would
            // take down the host process, and there is nowhere left to report it.
        }
    }
}
