using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Kekiri.Xunit;

namespace Assurance.UnitTests.RunnerTests;

// Neither side can report "met" unless the other is already running, so a run that executes one
// implementation after the other times out here rather than shaking out as a slow pass.
static class Rendezvous
{
    // long enough that a loaded CI box still schedules both implementations, short enough that a
    // regression to sequential execution fails the run rather than hanging it
    static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public const string Met = "met";

    public static string Meet(ManualResetEventSlim mine, ManualResetEventSlim theirs)
    {
        mine.Set();
        return theirs.Wait(Timeout) ? Met : "alone";
    }
}

public class ConcurrencyTests : Scenarios<TestingContext>
{
    readonly ManualResetEventSlim _existingStarted = new();
    readonly ManualResetEventSlim _replacementStarted = new();

    [Scenario]
    public void Synchronous_implementations_run_concurrently()
    {
        Given(each_implementation_waits_for_the_other);
        WhenAsync(implementations_are_run);
        Then(both_implementations_were_running_at_once);
    }

    void each_implementation_waits_for_the_other()
    {
        Context.Existing = () => Rendezvous.Meet(_existingStarted, _replacementStarted);
        Context.Replacement = () => Rendezvous.Meet(_replacementStarted, _existingStarted);
    }

    async Task implementations_are_run()
    {
        Context.Result = await Runner.RunInParallel(
            "SyncConcurrencyTests",
            Context.Existing,
            Context.Replacement);
    }

    void both_implementations_were_running_at_once()
    {
        Context.Result.Existing.Should().Be(Rendezvous.Met);
        Context.Result.Replacement.Should().Be(Rendezvous.Met);
    }
}

public class AsyncConcurrencyTests : Scenarios<AsyncTestingContext>
{
    readonly ManualResetEventSlim _existingStarted = new();
    readonly ManualResetEventSlim _replacementStarted = new();

    [Scenario]
    public void Asynchronous_implementations_run_concurrently()
    {
        Given(each_implementation_waits_for_the_other);
        WhenAsync(implementations_are_run);
        Then(both_implementations_were_running_at_once);
    }

    void each_implementation_waits_for_the_other()
    {
        Context.Existing = () => Task.Run(() => Rendezvous.Meet(_existingStarted, _replacementStarted));
        Context.Replacement = () => Task.Run(() => Rendezvous.Meet(_replacementStarted, _existingStarted));
    }

    async Task implementations_are_run()
    {
        Context.Result = await Runner.RunInParallel(
            "AsyncConcurrencyTests",
            Context.Existing,
            Context.Replacement);
    }

    void both_implementations_were_running_at_once()
    {
        Context.Result.Existing.Should().Be(Rendezvous.Met);
        Context.Result.Replacement.Should().Be(Rendezvous.Met);
    }
}
