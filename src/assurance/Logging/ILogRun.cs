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

    /// <summary>Records a field on this run.</summary>
    void Log(string field, object value);

    /// <summary>Adds to a field that can hold more than one value, such as a warning list.</summary>
    void AppendToValue(string field, string value);

    /// <summary>
    /// Writes how the two implementations compared. This is the one member a custom log usually
    /// overrides; everything else exists so <see cref="Runner"/> can drive the run.
    /// </summary>
    void LogRunResult(RunResult<T> result);

    /// <summary>Raises this run to a warning, with the reason.</summary>
    void Warn(string message);

    /// <summary>Whether <see cref="Complete"/> has already run.</summary>
    bool WasCompleted { get; }

    /// <summary>
    /// Called once the outcome is known. Implementations that own their
    /// <see cref="EventContext"/> should dispose it here. Must be idempotent, and safe to call
    /// from a finalizer thread while another thread is calling it too.
    /// </summary>
    void Complete();
}
