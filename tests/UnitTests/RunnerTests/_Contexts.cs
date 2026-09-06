using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spiffy.Monitoring;

namespace Assurance.UnitTests.RunnerTests;

public class TestingContext
{
    public Func<string> Existing { get; set; }
    public Func<string> Replacement { get; set; }
    public RunResult<string> Result { get; set; }

    // Spiffy keeps only the most recently installed provider, so scenarios running side by side
    // cannot each hold their own sink; they share this one and find their event by task name
    static readonly ConcurrentBag<LogEvent> LoggedEvents = new();

    static readonly TimeSpan FinalizationTimeout = TimeSpan.FromSeconds(5);
    static readonly TimeSpan FinalizationPollInterval = TimeSpan.FromMilliseconds(25);

    public TestingContext()
    {
        Configuration.Initialize(c =>
            c.Providers.Add("custom", evt => LoggedEvents.Add(evt)));
    }

    public LogEvent EventFor(string taskName)
    {
        var matches = LoggedEvents.Where(e => e.Properties["Operation"] == taskName).ToArray();
        if (matches.Length == 0)
        {
            throw new InvalidOperationException(
                $"No logged event named '{taskName}'. Either the run never emitted one, or its " +
                "result was never closed out.");
        }
        if (matches.Length > 1)
        {
            throw new InvalidOperationException(
                $"Found {matches.Length} logged events named '{taskName}'. The sink is shared " +
                "across scenarios, so a task name asserted on has to be unique across the suite.");
        }
        return matches[0];
    }

    // an abandoned result only writes its event from the finalizer, so a scenario asserting on
    // one has to drive collection rather than wait for it
    public void ForceFinalization()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    // a forced collection is not obliged to reclaim any particular object, so waiting on one
    // result's finalizer polls to a deadline rather than asserting on the first attempt. On
    // timeout EventFor reports the miss, so a finalizer that never runs still fails the scenario.
    public LogEvent AwaitEventFor(string taskName)
    {
        var deadline = DateTime.UtcNow + FinalizationTimeout;
        while (true)
        {
            ForceFinalization();
            if (HasEventFor(taskName) || DateTime.UtcNow >= deadline)
            {
                return EventFor(taskName);
            }
            Thread.Sleep(FinalizationPollInterval);
        }
    }

    bool HasEventFor(string taskName)
    {
        return LoggedEvents.Any(e => e.Properties["Operation"] == taskName);
    }
}

public class AsyncTestingContext
{
    public Func<Task<string>> Existing { get; set; }
    public Func<Task<string>> Replacement { get; set; }
    public RunResult<string> Result { get; set; }
}
