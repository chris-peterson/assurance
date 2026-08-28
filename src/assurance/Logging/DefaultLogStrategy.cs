using Spiffy.Monitoring;

namespace Assurance.Logging;

public class DefaultLogStrategy<T> : ILogStrategy<T>
{
    public DefaultLogStrategy(EventContext eventContext = null)
    {
        ProvidedEventContext = eventContext;
    }

    protected EventContext ProvidedEventContext { get; }

    public virtual ILogRun<T> Begin(string taskName)
    {
        return new DefaultLogRun<T>(ProvidedEventContext, taskName);
    }
}
