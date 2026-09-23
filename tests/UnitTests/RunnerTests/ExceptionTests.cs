using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using Assurance.Logging;
using Kekiri.Xunit;
using Spiffy.Monitoring;

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

    [Scenario]
    public void BothThrowIsLoggedAsAnError()
    {
        Given(existing_throws)
            .And(replacement_throws)
            .And(the_failing_run_has_its_own_task_name);
        WhenAsync(implementations_are_run).Throws();
        Then(the_run_is_logged_as_an_error);
    }

    [Scenario]
    public void ReplacementThrowingAloneIsNotAnError()
    {
        Given(replacement_throws)
            .But(existing_succeeds)
            .And(the_replacement_only_run_has_its_own_task_name);
        WhenAsync(implementations_are_run);
        Then(the_run_is_not_logged_as_an_error);
    }

    [Scenario]
    public void ReplacementThrowingLeavesTheCallersLevelAlone()
    {
        Given(replacement_throws)
            .But(existing_succeeds)
            .And(the_caller_raised_their_own_event_to_error);
        WhenAsync(the_run_records_into_the_callers_event);
        Then(the_callers_level_survives_the_run);
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

    const string ThrowingExistingRun = "ThrowingExistingRun";
    const string BothThrowingRun = "BothThrowingRun";
    const string ThrowingReplacementRun = "ThrowingReplacementRun";

    void the_run_has_its_own_task_name()
    {
        _taskName = ThrowingExistingRun;
    }

    void the_failing_run_has_its_own_task_name()
    {
        _taskName = BothThrowingRun;
    }

    void the_replacement_only_run_has_its_own_task_name()
    {
        _taskName = ThrowingReplacementRun;
    }

    readonly EventContext _callersContext = new();

    void the_caller_raised_their_own_event_to_error()
    {
        _callersContext.SetToError("the caller's own problem");
    }

    async Task the_run_records_into_the_callers_event()
    {
        Context.Result = await Runner.RunInParallel(
            "CallersLevelRun",
            Context.Existing,
            Context.Replacement,
            new DefaultLogStrategy<string>(_callersContext));
        Context.Result.UseExisting();
    }

    void the_callers_level_survives_the_run()
    {
        // a replacement failure is shielded from the caller, so the runner puts the level back
        // where it found it rather than forcing it down to Info
        _callersContext.Level.Should().Be(Level.Error);
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
        var properties = Context.EventFor(ThrowingExistingRun).Properties;
        properties["Existing_Message"].Should().Contain("from existing");
        properties.Should().ContainKey("TimeElapsed_Existing");
        properties.Should().ContainKey("TimeElapsed_Replacement");
    }

    // the two implementations no longer finish in a fixed order, so the level has to be decided
    // once both are done rather than by whichever wrote to the context last
    void the_run_is_logged_as_an_error()
    {
        Catch<Exception>();
        Context.EventFor(BothThrowingRun).Level.Should().Be(Level.Error);
    }

    void the_run_is_not_logged_as_an_error()
    {
        // the event is written when the run is closed out, so pick a side before reading it
        Context.Result.UseExisting();
        Context.EventFor(ThrowingReplacementRun).Level.Should().Be(Level.Info);
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
