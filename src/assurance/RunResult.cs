using System;
using Assurance.Compare;
using Assurance.Logging;
using Spiffy.Monitoring;

namespace Assurance;

public class RunResult<T>
{
    readonly ILogStrategy<T> _logstrategy;

    internal RunResult(T existing, T replacement, IComparisonStrategy<T> comparisonStrategy, ILogStrategy<T> logStrategy)
    {
        Existing = existing;
        Replacement = replacement;
        _logstrategy = logStrategy;
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

    public EventContext EventContext => _logstrategy.EventContext;

    void LogUse(string use)
    {
        _logstrategy.Log("Use", use);
        _logstrategy.Finalize();
    }

    ~RunResult()
    {
        if (!_logstrategy.WasFinalized)
        {
            _logstrategy.Warn("Call UseExisting or UseReplacement in order to avoid this warning");
            LogUse("unknown");
        }
    }
}
