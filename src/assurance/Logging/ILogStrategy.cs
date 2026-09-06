namespace Assurance.Logging;

/// <summary>
/// What a run logs. Implement this to decide what <see cref="Runner"/> writes about a comparison,
/// or to send it somewhere Spiffy does not reach.
/// </summary>
public interface ILogStrategy<T>
{
    /// <summary>
    /// Starts a run and returns the log it records into. <see cref="Runner"/> calls this once per
    /// run, so a strategy shared across runs still reports each one in full.
    /// </summary>
    ILogRun<T> Begin(string taskName);
}
