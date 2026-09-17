using System.Threading;
using System.Threading.Tasks;
using Assurance.UnitTests.TestModels;
using AwesomeAssertions;
using Kekiri.Xunit;

namespace Assurance.UnitTests.RunnerTests;

public class ConcurrencyTests : Scenarios<TestingContext>
{
    readonly ManualResetEventSlim _existingStarted = new();
    readonly ManualResetEventSlim _replacementStarted = new();

    [Scenario]
    public void SynchronousImplementationsRunConcurrently()
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
            "ConcurrencyTests",
            Context.Existing,
            Context.Replacement);
    }

    void both_implementations_were_running_at_once()
    {
        Context.Result.Existing.Should().Be(Rendezvous.Met);
        Context.Result.Replacement.Should().Be(Rendezvous.Met);
    }
}
