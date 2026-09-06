using System.Threading;
using Spiffy.Monitoring;

namespace Assurance.Logging;

/// <summary>
/// The log a run records into when the caller has not supplied one of their own. Override
/// <see cref="LogRunResult"/> to change what a run writes about the comparison.
/// </summary>
public class DefaultLogRun<T> : ILogRun<T>
{
    readonly bool _isMyEventContext;
    int _completed;

    /// <param name="eventContext">
    /// The caller's context to record into, or null to open one for this run alone and dispose it
    /// on <see cref="Complete"/>. The fields this run writes into a caller's context are prefixed;
    /// the per-implementation timings and exception details Spiffy writes are not, so a caller
    /// already timing a step called "Existing" or "Replacement" shares those names.
    /// </param>
    /// <param name="taskName">Names the run in the log.</param>
    public DefaultLogRun(EventContext eventContext, string taskName)
    {
        _isMyEventContext = eventContext == null;

        if (_isMyEventContext)
        {
            EventContext = new EventContext(AssuranceLog.Component, taskName);
        }
        else
        {
            EventContext = eventContext;
            EventContext[GetLoggingKey("Task")] = taskName;
        }
    }

    public EventContext EventContext { get; }

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

    public void Warn(string message)
    {
        EventContext.SetToWarning(message);
    }

    public bool WasCompleted => Volatile.Read(ref _completed) == 1;

    // a result's finalizer can reach this while the thread that owns the run is calling it, so the
    // check and the set have to be one operation
    public void Complete()
    {
        if (Interlocked.Exchange(ref _completed, 1) == 1)
        {
            return;
        }
        if (_isMyEventContext)
        {
            EventContext.Dispose();
        }
    }

    string GetLoggingKey(string key)
    {
        return _isMyEventContext ? key : $"{AssuranceLog.Component}{key}";
    }
}
