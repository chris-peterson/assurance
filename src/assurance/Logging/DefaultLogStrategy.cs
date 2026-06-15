using Spiffy.Monitoring;

namespace Assurance.Logging;

public class DefaultLogStrategy<T> : ILogStrategy<T>
{
    private EventContext _eventContext; 
    private string _loggingPrefix; 
    private bool _isMyEventContext;

    public EventContext EventContext => _eventContext;
    public bool WasFinalized { get; internal set; }

    public DefaultLogStrategy(string taskName, EventContext eventContext = null)
    {
        _loggingPrefix = null;
        _isMyEventContext = false;
        if (eventContext == null)
        {
            _isMyEventContext = true;
            eventContext = new EventContext("Assurance", taskName);
        }
        else
        {
            _loggingPrefix = "Assurance";
            eventContext[$"{_loggingPrefix}Task"] = taskName;
        }
        _eventContext = eventContext;
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

#pragma warning disable CS0465 // Introducing a 'Finalize' method can interfere with destructor invocation
    public void Finalize()
#pragma warning restore CS0465 // Introducing a 'Finalize' method can interfere with destructor invocation
    {
        if (_isMyEventContext)
        {
            _eventContext.Dispose();
        }
        WasFinalized = true;
    }

    public void Warn(string value)
    {
        _eventContext.SetToWarning(value);
    }
}