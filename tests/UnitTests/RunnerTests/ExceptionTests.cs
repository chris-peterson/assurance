using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using Kekiri.Xunit;

namespace Assurance.UnitTests.RunnerTests;

public class ExceptionTests : Scenarios<TestingContext>
{
    [Scenario]
    public void ExistingThrows()
    {
        Given(existing_throws);
        WhenAsync(implementations_are_run).Throws();
        Then(an_exception_is_raised);
    }

    [Scenario]
    public void ExistingThrowsIsStillLogged()
    {
        Given(existing_throws)
            .And(the_run_has_its_own_task_name);
        WhenAsync(implementations_are_run).Throws();
        Then(the_failed_run_is_logged);
    }

    [Scenario]
    public void ReplacementThrows()
    {
        Given(replacement_throws)
            .But(existing_succeeds);
        WhenAsync(implementations_are_run);
        Then(existing_result_is_used);
    }

    [Scenario]
    public void BothThrow()
    {
        Given(existing_throws)
            .And(replacement_throws);
        WhenAsync(implementations_are_run).Throws();
        Then(an_exception_is_raised);
    }

    void existing_throws()
    {
        Context.Existing = () => throw new Exception("from existing");
    }

    void existing_succeeds()
    {
        Context.Existing = () => "foo";
    }

    void replacement_throws()
    {
        Context.Replacement = () => throw new Exception("from replacement");
    }

    // EventFor matches on task name, so a scenario asserting on the emitted event needs its own
    string _taskName = "ExceptionTests";

    void the_run_has_its_own_task_name()
    {
        _taskName = "ThrowingExistingRun";
    }

    async Task implementations_are_run()
    {
        Context.Result = await Runner.RunInParallel(
            _taskName,
            Context.Existing,
            Context.Replacement);
    }

    void the_failed_run_is_logged()
    {
        Catch<Exception>();
        var properties = Context.EventFor(_taskName).Properties;
        properties["Existing_Message"].Should().Contain("from existing");
        properties.Should().ContainKey("TimeElapsed_Existing");
        properties.Should().ContainKey("TimeElapsed_Replacement");
    }

    void an_exception_is_raised()
    {
        Catch<Exception>()
            .Message.Should().Be("from existing");
    }

    void existing_result_is_used()
    {
        Context.Result.Existing.Should().Be("foo");
        Context.Result.Replacement.Should().BeNull();
        Context.Result.ResultComparison.AreEqual.Should().BeFalse();
    }
}
