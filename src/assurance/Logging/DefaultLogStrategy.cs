using Spiffy.Monitoring;

namespace Assurance.Logging;

/// <summary>
/// The log <see cref="Runner"/> uses when the caller names no strategy. Override
/// <see cref="Begin"/> to return a <see cref="DefaultLogRun{T}"/> subclass of your own.
/// </summary>
public class DefaultLogStrategy<T> : ILogStrategy<T>
{
    /// <param name="eventContext">
    /// The caller's context to record into, so an Assurance run correlates with the event around
    /// it. A strategy built this way writes every run it begins into that one event, so give it to
    /// one run at a time; the parameterless form opens a fresh event per run and can be shared
    /// across concurrent runs.
    /// </param>
    public DefaultLogStrategy(EventContext eventContext = null)
    {
        ProvidedEventContext = eventContext;
    }

    /// <summary>The caller's context, or null when each run opens its own event.</summary>
    protected EventContext ProvidedEventContext { get; }

    public virtual ILogRun<T> Begin(string taskName)
    {
        return new DefaultLogRun<T>(ProvidedEventContext, taskName);
    }
}
