using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Kekiri.Xunit;

namespace Assurance.UnitTests.RunnerTests;

public class RunToCompletionTests : Scenarios<TestingContext>
{
    [Scenario]
    public void UseExisting()
    {
        Given(different_results);
        WhenAsync(implementations_are_run);
        Then(existing_result_is_used);
    }

    [Scenario]
    public void UseReplacement()
    {
        Given(different_results);
        WhenAsync(implementations_are_run);
        Then(replacement_result_is_used);
    }

    [Scenario]
    public void SameResult()
    {
        Given(implementations_have_same_result);
        WhenAsync(implementations_are_run);
        Then(the_results_are_identical);
    }

    [Scenario]
    public void BothNull()
    {
        WhenAsync(implementations_are_run);
        Then(the_result_is_null);
    }

    [Scenario]
    public void AmbiguousUse()
    {
        Given(multiple_implementations)
            .But(source_to_use_is_not_specified)
            .And(the_run_has_its_own_task_name);
        WhenAsync(implementations_are_run);
        Then(a_warning_is_logged);
    }

    const string TestString = "foo";
    void implementations_have_same_result()
    {
        Context.Existing = () =>
        {
            Thread.Sleep(10);
            return TestString;
        };
        Context.Replacement = () =>
        {
            Thread.Sleep(10);
            return TestString;
        };
    }

    void different_results()
    {
        Context.Existing = () => TestString;
        Context.Replacement = () => TestString + "something else";
    }

    // EventFor matches on task name, so a scenario asserting on the emitted event needs its own
    string _taskName = "RunToCompletionTests";

    void the_run_has_its_own_task_name()
    {
        _taskName = "AbandonedResultRun";
    }

    async Task implementations_are_run()
    {
        Context.Result = await Runner.RunInParallel(
            _taskName,
            Context.Existing,
            Context.Replacement);
    }

    void the_results_are_identical()
    {
        Context.Result.ResultComparison.AreEqual.Should().BeTrue();
        Context.Result.Existing.Should().BeSameAs(Context.Result.Replacement);
        Context.Result.Existing.Should().BeSameAs(TestString);
        Context.Result.Replacement.Should().BeSameAs(TestString);
    }

    void existing_result_is_used()
    {
        var result = Context.Result.UseExisting();
        result.Should().BeSameAs(Context.Existing());
        result.Should().NotBe(Context.Replacement());
        Context.Result.ResultComparison.AreEqual.Should().BeFalse();
    }

    void replacement_result_is_used()
    {
        var result = Context.Result.UseReplacement();
        result.Should().BeSameAs(Context.Replacement());
        result.Should().NotBe(Context.Existing());
        Context.Result.ResultComparison.AreEqual.Should().BeFalse();
    }

    void the_result_is_null()
    {
        Context.Result.Existing.Should().BeNull();
        Context.Result.Replacement.Should().BeNull();
        Context.Result.ResultComparison.AreEqual.Should().BeTrue();
    }

    void multiple_implementations()
    {
        // for scenario readability
    }

    void source_to_use_is_not_specified()
    {
        // didn't call Context.Result UseExisting/UseReplacement
    }

    void a_warning_is_logged()
    {
        // coerce finalization
        Context.Result = null;

        var props = Context.AwaitEventFor(_taskName).Properties;
        props["Use"].Should().Be("unknown");
        props["WarningReason"].Should().Contain("UseExisting");
        props["WarningReason"].Should().Contain("UseReplacement");
    }
}
