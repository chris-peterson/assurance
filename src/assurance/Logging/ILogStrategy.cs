namespace Assurance.Logging;

public interface ILogStrategy<T>
{
    /// <summary>
    /// Starts a run and returns the log it records into. <see cref="Runner"/> calls this once per
    /// run, so a strategy shared across runs still reports each one in full.
    /// </summary>
    ILogRun<T> Begin(string taskName);
}
