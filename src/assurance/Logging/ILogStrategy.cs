using Spiffy.Monitoring;

namespace Assurance.Logging;

public interface ILogStrategy<T>
{
    EventContext EventContext { get; }
    
    void AppendToValue(string v1, string v2);
    void Log(string field, object value);
    void LogRunResult(RunResult<T> result);
    void Warn(string message);
    
    bool WasFinalized { get; }
    #pragma warning disable CS0465 // Introducing a 'Finalize' method can interfere with destructor invocation
    void Finalize();
    #pragma warning restore CS0465 // Introducing a 'Finalize' method can interfere with destructor invocation
}