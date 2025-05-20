using System;
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

        public TestingContext()
        {
            Configuration.Initialize(c =>
                c.Providers.Add("custom", evt => {
                    LoggedEvent = evt;
                    WaitForLogEvent.Set();
                }));
        }
    }
    
    public class AsyncTestingContext
    {
        public Func<Task<string>> Existing { get; set; }
        public Func<Task<string>> Replacement { get; set; }
        public RunResult<string> Result { get; set; }
    }

}
