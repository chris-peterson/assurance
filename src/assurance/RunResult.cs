using System;
using Assurance.Compare;
using Spiffy.Monitoring;

namespace Assurance;

public class RunResult<T>
{
    readonly LoggingContext _loggingContext;

    internal RunResult(T existing, T replacement, IComparisonStrategy<T> comparisonStrategy, LoggingContext loggingContext)
    {
        Existing = existing;
        Replacement = replacement;
        _loggingContext = loggingContext;
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

    public EventContext EventContext => _loggingContext.EventContext;

    void LogUse(string use)
    {
        _loggingContext.Log("Use", use);
        _loggingContext.Finalize();
    }

    ~RunResult()
    {
        if (!_loggingContext.WasFinalized)
        {
            _loggingContext.Warn("Call UseExisting or UseReplacement in order to avoid this warning");
            LogUse("unknown");
        }
    }
}
