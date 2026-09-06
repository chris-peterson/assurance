using System.Threading;
using System.Threading.Tasks;
using Assurance.UnitTests.TestModels;
using AwesomeAssertions;
using Kekiri.Xunit;

namespace Assurance.UnitTests.RunnerTests;

public class AsyncConcurrencyTests : Scenarios<AsyncTestingContext>
{
    readonly ManualResetEventSlim _existingStarted = new();
    readonly ManualResetEventSlim _replacementStarted = new();

    [Scenario]
    public void AsynchronousImplementationsRunConcurrently()
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
