using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spiffy.Monitoring;

namespace Assurance.UnitTests.RunnerTests
{
    public class TestingContext
    {
        public Func<string> Existing { get; set; }
        public Func<string> Replacement { get; set; }
        public RunResult<string> Result { get; set; }
        public LogEvent LoggedEvent { get; set; }
        
        public ManualResetEvent WaitForLogEvent = new ManualResetEvent(false);

        // scenarios run side by side and each installs its own provider, so events go to a shared
        // sink; a scenario finds its own by the task name it ran under
        static readonly ConcurrentBag<LogEvent> LoggedEvents = new();

        public TestingContext()
        {
            InstallLogProvider();
        }

        // Spiffy keeps only the most recently installed provider, and Kekiri builds the context
        // lazily, so a scenario reading events installs one itself rather than relying on another
        // scenario having built a context first
        public void InstallLogProvider()
        {
            Configuration.Initialize(c =>
                c.Providers.Add("custom", evt => {
                    LoggedEvents.Add(evt);
                    LoggedEvent = evt;
                    WaitForLogEvent.Set();
                }));
        }

        public LogEvent EventFor(string taskName)
        {
            return LoggedEvents.Single(e => e.Properties["Operation"] == taskName);
        }
    }
    
    public class AsyncTestingContext
    {
        public Func<Task<string>> Existing { get; set; }
        public Func<Task<string>> Replacement { get; set; }
        public RunResult<string> Result { get; set; }
    }

}
