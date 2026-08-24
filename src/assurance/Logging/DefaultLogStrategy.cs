using System;
using Spiffy.Monitoring;

namespace Assurance.Logging;

public class DefaultLogStrategy<T> : ILogStrategy<T>
{
    readonly EventContext _providedEventContext;
    readonly bool _isMyEventContext;
    readonly string _loggingPrefix;

    EventContext _eventContext;
    bool _begun;

    public DefaultLogStrategy(EventContext eventContext = null)
    {
        _providedEventContext = eventContext;
        _isMyEventContext = eventContext == null;
        _loggingPrefix = _isMyEventContext ? null : "Assurance";
    }

    public EventContext EventContext => _eventContext;

    public bool WasCompleted { get; private set; }

    public void Begin(string taskName)
    {
        if (_begun)
        {
            throw new InvalidOperationException(
                $"{GetType().Name} is single-use because it owns the lifetime of one log event; " +
                "construct one per run instead of sharing an instance.");
        }
        _begun = true;

        if (_isMyEventContext)
        {
            _eventContext = new EventContext("Assurance", taskName);
        }
        else
        {
            _eventContext = _providedEventContext;
            _eventContext[GetLoggingKey("Task")] = taskName;
        }
    }

    string GetLoggingKey(string key)
    {
        return $"{_loggingPrefix}{key}";
    }

    public void Log(string field, object value)
    {
        _eventContext[GetLoggingKey(field)] = value;
    }

    public void AppendToValue(string field, string value)
    {
        _eventContext.AppendToValue(GetLoggingKey(field), value, ",");
    }

    public virtual void LogRunResult(RunResult<T> result)
    {
        if (result.ResultComparison.AreEqual)
        {
            Log("Result", "same");
        }
        else
        {
            Log("Result", "different");
            Log("Differences", result.ResultComparison.Differences);
        }
    }

    public void Complete()
    {
        if (WasCompleted)
        {
            return;
        }
        WasCompleted = true;
        if (_isMyEventContext)
        {
            _eventContext.Dispose();
        }
    }

    public void Warn(string value)
    {
        _eventContext.SetToWarning(value);
    }
}
