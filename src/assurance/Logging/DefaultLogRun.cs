using Spiffy.Monitoring;

namespace Assurance.Logging;

public class DefaultLogRun<T> : ILogRun<T>
{
    // fields written into a caller's own context are namespaced, so nothing of theirs is overwritten
    internal const string CallerContextPrefix = "Assurance";

    readonly bool _isMyEventContext;
    readonly string _loggingPrefix;

    public DefaultLogRun(EventContext eventContext, string taskName)
    {
        _isMyEventContext = eventContext == null;
        _loggingPrefix = _isMyEventContext ? null : CallerContextPrefix;

        if (_isMyEventContext)
        {
            EventContext = new EventContext("Assurance", taskName);
        }
        else
        {
            EventContext = eventContext;
            EventContext[GetLoggingKey("Task")] = taskName;
        }
    }

    public EventContext EventContext { get; }

    public bool WasCompleted { get; private set; }

    public void Log(string field, object value)
    {
        EventContext[GetLoggingKey(field)] = value;
    }

    public void AppendToValue(string field, string value)
    {
        EventContext.AppendToValue(GetLoggingKey(field), value, ",");
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
            EventContext.Dispose();
        }
    }

    public void Warn(string value)
    {
        EventContext.SetToWarning(value);
    }

    string GetLoggingKey(string key)
    {
        return $"{_loggingPrefix}{key}";
    }
}
