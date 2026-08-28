using Spiffy.Monitoring;

namespace Assurance.Logging;

/// <summary>
/// One run's log. Everything here is per-run state, so a result finalized long after its run
/// reports against its own log rather than whatever the strategy has moved on to.
/// </summary>
public interface ILogRun<T>
{
    /// <summary>
    /// Where this run records, or null when the log writes somewhere <see cref="Runner"/> cannot
    /// reach; it times both implementations against this context.
    /// </summary>
    EventContext EventContext { get; }

    void AppendToValue(string field, string value);
    void Log(string field, object value);
    void LogRunResult(RunResult<T> result);
    void Warn(string message);

    bool WasCompleted { get; }

    /// <summary>
    /// Called once the outcome is known. Implementations that own their
    /// <see cref="EventContext"/> should dispose it here. Must be idempotent.
    /// </summary>
    void Complete();
}
