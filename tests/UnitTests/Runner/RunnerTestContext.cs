using System;
using System.Threading;
using Spiffy.Monitoring;

namespace Assurance.UnitTests
{
    public class RunnerTestContext
    {
        public Func<string> Existing { get; set; }
        public Func<string> Replacement { get; set; }
        public RunResult<string> Result { get; set; }
        public LogEvent LoggedEvent { get; set; }
        public ManualResetEvent WaitForLogEvent = new ManualResetEvent(false);

        public RunnerTestContext()
        {
            Configuration.Initialize(c =>
                c.Providers.Add("custom", evt => {
                    LoggedEvent = evt;
                    WaitForLogEvent.Set();
                }));
        }
    }
}
