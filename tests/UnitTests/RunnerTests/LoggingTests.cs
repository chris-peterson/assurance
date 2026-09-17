using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Assurance.Logging;
using Assurance.UnitTests.TestModels;
using AwesomeAssertions;
using Kekiri.Xunit;
using Spiffy.Monitoring;

namespace Assurance.UnitTests.RunnerTests;

public class LoggingTests : Scenarios<TestingContext>
{
    EventContext _myContext = new();
    ModifiedLogStrategy _myLogStrategy;
    FinalizerAwareLogStrategy _finalizerAwareLogStrategy;
    RunResult<List<string>> _listResult;

    [Scenario]
    public void Use_own_event_context()
    {
        WhenAsync(using_my_event_context);
        Then(logged_fields_are_namespaced);
    }

    [Scenario]
    public void Use_generated_event_context()
    {
        WhenAsync(event_context_not_specified);
        Then(logged_fields_are_NOT_namespaced);
    }

    [Scenario]
    public void Default_logging_outputs_existing_use_when_chosen()
    {
        WhenAsync(existing_use_is_chosen_after_a_comparison);
        Then(default_existing_use_is_logged);
    }

    [Scenario]
    public void Default_logging_outputs_replacement_use_when_chosen()
    {
        WhenAsync(replacement_use_is_chosen_after_a_comparison);
        Then(default_replacement_use_is_logged);
    }

    [Scenario]
    public void Default_logging_outputs_same_result()
    {
        Given(same_values_for_compare);
        WhenAsync(event_context_not_specified);
        Then(default_same_output_is_logged);
    }

    [Scenario]
    public void Default_logging_outputs_different_result()
    {
        Given(different_values_for_compare);
        WhenAsync(event_context_not_specified);
        Then(default_difference_output_is_logged);
    }

    [Scenario]
    public void Custom_logging_outputs_same_result()
    {
        Given(a_custom_log_strategy);
        WhenAsync(same_list_results_are_compared);
        Then(custom_same_output_is_logged);
    }

    [Scenario]
    public void Custom_logging_outputs_different_result()
    {
        Given(a_custom_log_strategy);
        WhenAsync(different_list_results_are_compared);
        Then(custom_difference_output_is_logged);
    }

    [Scenario]
    public void Custom_log_strategy_is_given_the_runners_task_name()
    {
        Given(a_custom_log_strategy);
        WhenAsync(same_list_results_are_compared);
        Then(the_runners_task_name_is_logged);
    }

    [Scenario]
    public void Reusing_a_log_strategy_across_runs_starts_a_new_event()
    {
        Given(a_custom_log_strategy);
        WhenAsync(the_same_log_strategy_is_used_twice);
        Then(the_second_run_is_logged_in_full);
    }

    [Scenario]
    public void A_strategy_holding_an_event_context_writes_every_run_into_it()
    {
        Given(a_custom_log_strategy_on_my_event_context);
        WhenAsync(the_same_log_strategy_is_used_twice);
        Then(my_event_context_holds_the_last_run_alone);
    }

    [Scenario]
    public void Each_run_on_a_shared_log_strategy_is_emitted_on_its_own()
    {
        Given(a_custom_log_strategy);
        WhenAsync(an_abandoned_run_is_finalized_after_a_later_one);
        Then(the_abandoned_run_is_emitted_with_an_unknown_use);
    }

    [Scenario]
    public void A_finalized_result_leaves_a_later_run_on_the_same_strategy_alone()
    {
        Given(a_custom_log_strategy);
        WhenAsync(an_earlier_result_is_finalized_during_a_later_run);
        Then(the_earlier_run_was_closed_out_by_its_finalizer)
            .And(the_later_run_reports_its_own_use);
    }

    [Scenario]
    public void A_completed_result_does_not_reach_its_log_again_when_collected()
    {
        Given(a_finalizer_aware_log_strategy);
        WhenAsync(a_completed_result_is_collected);
        Then(the_log_is_not_consulted_after_the_run_was_closed_out);
    }

    [Scenario]
    public void A_log_strategy_without_an_event_context_still_runs()
    {
        WhenAsync(a_strategy_with_no_event_context_is_supplied);
        Then(the_run_completes_without_an_event_context);
    }

    [Scenario]
    public void An_undefined_implementation_is_reported_when_the_strategy_has_no_event_context()
    {
        WhenAsync(a_strategy_with_no_event_context_runs_an_undefined_implementation);
        Then(the_undefined_implementation_is_reported_where_the_timings_are);
    }

    void same_values_for_compare()
    {
        Context.Existing = () => "foo";
        Context.Replacement = () => "foo";
    }

    void different_values_for_compare()
    {
        Context.Existing = () => "foo";
        Context.Replacement = () => "doo";
    }

    void a_custom_log_strategy()
    {
        _myLogStrategy = new ModifiedLogStrategy();
    }

    void a_custom_log_strategy_on_my_event_context()
    {
        _myLogStrategy = new ModifiedLogStrategy(_myContext);
    }

    void a_finalizer_aware_log_strategy()
    {
        _finalizerAwareLogStrategy = new FinalizerAwareLogStrategy();
    }

    Task<RunResult<List<string>>> a_run_named(string taskName, string replacement)
    {
        return Runner.RunInParallel(
            taskName,
            () => new List<string> { "1" },
            () => new List<string> { replacement },
            logStrategy: _myLogStrategy);
    }

    async Task using_my_event_context()
    {
        Context.Result = await Runner.RunInParallel(
            "TheTaskName",
            Context.Existing,
            Context.Replacement,
            new DefaultLogStrategy<string>(_myContext));
    }

    async Task event_context_not_specified()
    {
        Context.Result = await Runner.RunInParallel(
            "TheTaskName",
            Context.Existing,
            Context.Replacement);
    }

    async Task same_list_results_are_compared()
    {
        _listResult = await Runner.RunInParallel(
            "SameLists",
            () => new List<string> { "1", "2", "3" },
            () => new List<string> { "1", "2", "3" },
            logStrategy: _myLogStrategy);
    }

    async Task different_list_results_are_compared()
    {
        _listResult = await Runner.RunInParallel(
            "DifferentLists",
            () => new List<string> { "1", "2", "3", "5", "4" },
            () => new List<string> { "1", "2", "3", "4", "5" },
            logStrategy: _myLogStrategy);
    }

    async Task the_same_log_strategy_is_used_twice()
    {
        await same_list_results_are_compared();
        await different_list_results_are_compared();
    }

    async Task an_earlier_run_is_abandoned()
    {
        await a_run_named("EarlierRun", "1");
    }

    async Task an_earlier_result_is_finalized_during_a_later_run()
    {
        // the earlier result is unreachable once its own frame is gone, and it was never told
        // which side to use, so the finalizer is what closes it out
        await an_earlier_run_is_abandoned();

        _listResult = await a_run_named("LaterRun", "2");
        Context.AwaitEventFor("EarlierRun");
        _listResult.UseReplacement();
    }

    async Task an_abandoned_run_is_left_behind()
    {
        await a_run_named("AbandonedRun", "1");
    }

    async Task an_abandoned_run_is_finalized_after_a_later_one()
    {
        // the abandoned result is unreachable once its own frame is gone, so it can be finalized
        await an_abandoned_run_is_left_behind();

        _listResult = await a_run_named("FollowingRun", "2");
        Context.AwaitEventFor("AbandonedRun");
    }

    async Task a_completed_run_is_left_to_the_collector()
    {
        var completed = await Runner.RunInParallel(
            "CollectedAfterCompleting",
            () => new List<string> { "1" },
            () => new List<string> { "1" },
            logStrategy: _finalizerAwareLogStrategy);
        completed.UseExisting();
    }

    async Task a_completed_result_is_collected()
    {
        await a_completed_run_is_left_to_the_collector();

        Context.ForceFinalization();
    }

    async Task a_strategy_with_no_event_context_runs_an_undefined_implementation()
    {
        Context.Result = await Runner.RunInParallel(
            "UndefinedExistingWithoutAContext",
            (Func<string>)null,
            () => "foo",
            logStrategy: new NullContextLogStrategy());
    }

    async Task a_strategy_with_no_event_context_is_supplied()
    {
        Context.Result = await Runner.RunInParallel(
            "NoEventContextRun",
            () => "foo",
            () => "foo",
            logStrategy: new NullContextLogStrategy());
    }

    async Task existing_use_is_chosen_after_a_comparison()
    {
        await event_context_not_specified();
        Context.Result.UseExisting();
    }

    async Task replacement_use_is_chosen_after_a_comparison()
    {
        await event_context_not_specified();
        Context.Result.UseReplacement();
    }

    void logged_fields_are_namespaced()
    {
        _myContext.Component.Should().NotBe("Assurance");
        _myContext.Operation.Should().NotBe("TheTaskName");
        _myContext["AssuranceTask"].Should().Be("TheTaskName");
        _myContext["AssuranceResult"].Should().Be("same");
    }

    void logged_fields_are_NOT_namespaced()
    {
        Context.Result.EventContext.Component.Should().Be("Assurance");
        Context.Result.EventContext.Operation.Should().Be("TheTaskName");
        Context.Result.EventContext.Contains("Task").Should().BeFalse();
        Context.Result.EventContext["Result"].Should().Be("same");
    }

    void default_existing_use_is_logged()
    {
        Context.Result.EventContext["Use"].Should().Be("existing");
    }

    void default_replacement_use_is_logged()
    {
        Context.Result.EventContext["Use"].Should().Be("replacement");
    }

    void default_same_output_is_logged()
    {
        Context.Result.EventContext["Result"].Should().Be("same");
        Context.Result.EventContext.Contains("Differences").Should().BeFalse();
    }

    void default_difference_output_is_logged()
    {
        Context.Result.EventContext["Result"].Should().Be("different");
        Context.Result.EventContext["Differences"].ToString().Should().Be("foo != doo");
    }

    void custom_same_output_is_logged()
    {
        _listResult.EventContext["Result"].Should().Be("equal");
        _listResult.EventContext.Contains("Differences").Should().BeFalse();
        _listResult.EventContext.Contains("ExistingFive").Should().BeFalse();
        _listResult.EventContext.Contains("ReplacementFive").Should().BeFalse();
    }

    void custom_difference_output_is_logged()
    {
        _listResult.EventContext["Result"].Should().Be("notEqual");
        _listResult.EventContext.Contains("Differences").Should().BeFalse();
        _listResult.EventContext["ExistingFive"].Should().Be("1,2,3,5,4");
        _listResult.EventContext["ReplacementFive"].Should().Be("1,2,3,4,5");
    }

    void the_runners_task_name_is_logged()
    {
        _listResult.EventContext.Operation.Should().Be("SameLists");
    }

    void my_event_context_holds_the_last_run_alone()
    {
        // one context is one event, so a strategy built on the caller's context serves one run;
        // the parameterless form opens a fresh event per run and can be shared
        _myContext["AssuranceTask"].Should().Be("DifferentLists");
        _myContext["AssuranceResult"].Should().Be("notEqual");
    }

    void the_abandoned_run_is_emitted_with_an_unknown_use()
    {
        var properties = Context.EventFor("AbandonedRun").Properties;
        properties["Result"].Should().Be("equal");
        properties["Use"].Should().Be("unknown");
        properties["WarningReason"].Should().Contain("UseExisting");
    }

    void the_earlier_run_was_closed_out_by_its_finalizer()
    {
        // without this the scenario passes whenever the collection didn't happen, which is the
        // one condition that would make the assertion below meaningless
        Context.EventFor("EarlierRun").Properties["Use"].Should().Be("unknown");
    }

    void the_later_run_reports_its_own_use()
    {
        var properties = Context.EventFor("LaterRun").Properties;
        properties["Use"].Should().Be("replacement");
        properties.Should().NotContainKey("WarningReason");
    }

    void the_log_is_not_consulted_after_the_run_was_closed_out()
    {
        _finalizerAwareLogStrategy.LastRun.CompletedChecks.Should().Be(0);
        Context.EventFor("CollectedAfterCompleting").Properties["Use"].Should().Be("existing");
    }

    void the_undefined_implementation_is_reported_where_the_timings_are()
    {
        Context.EventFor("UndefinedExistingWithoutAContext").Properties["Warnings"]
            .Should().Contain("Existing implementation is undefined");
    }

    void the_second_run_is_logged_in_full()
    {
        _listResult.EventContext["Result"].Should().Be("notEqual");
        _listResult.EventContext["ExistingFive"].Should().Be("1,2,3,5,4");
        _listResult.EventContext["ReplacementFive"].Should().Be("1,2,3,4,5");
    }

    void the_run_completes_without_an_event_context()
    {
        Context.Result.Existing.Should().Be("foo");
        Context.Result.ResultComparison.AreEqual.Should().BeTrue();
        Context.Result.EventContext.Should().BeNull();
    }
}
