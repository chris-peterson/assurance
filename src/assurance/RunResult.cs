using Spiffy.Monitoring;

namespace Assurance;

public class RunResult<T>
{
    readonly LoggingContext _loggingContext;
    internal RunResult(T existing, T replacement, LoggingContext loggingContext)
    {
        Existing = existing;
        Replacement = replacement;
        _loggingContext = loggingContext;
    }

    public T Existing { get; }
    public T Replacement { get; }
    public bool SameResult
    {
        get
        {
            if (Existing == null)
                return Replacement == null;
            return Existing.Equals(Replacement);
        }
    }

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
