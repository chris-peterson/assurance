using Spiffy.Monitoring;

namespace Assurance;

internal class LoggingContext(EventContext _eventContext, string _loggingPrefix, bool _isMyEventContext)
{
    public EventContext EventContext => _eventContext;
    string GetLoggingKey(string key)
    {
        return $"{_loggingPrefix}{key}";
    }

    public void Log(string field, object value)
    {
        _eventContext[GetLoggingKey(field)] = value;
    }

    public void AppendToValue(string field, string value)
    {
        _eventContext.AppendToValue(GetLoggingKey(field), value, ",");
    }

#pragma warning disable CS0465 // Introducing a 'Finalize' method can interfere with destructor invocation
    public void Finalize()
#pragma warning restore CS0465 // Introducing a 'Finalize' method can interfere with destructor invocation
    {
        if (_isMyEventContext)
        {
            _eventContext.Dispose();
        }
        WasFinalized = true;
    }

    public bool WasFinalized { get; set; }

    public void Warn(string value)
    {
        _eventContext.SetToWarning(value);
    }
}
