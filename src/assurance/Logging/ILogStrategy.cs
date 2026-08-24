using Spiffy.Monitoring;

namespace Assurance.Logging;

public interface ILogStrategy<T>
{
    EventContext EventContext { get; }

    /// <summary>
    /// Called by <see cref="Runner"/> once, before any other member, with the task name it was given.
    /// Implementations that own their <see cref="EventContext"/> should create it here.
    /// </summary>
    void Begin(string taskName);

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
