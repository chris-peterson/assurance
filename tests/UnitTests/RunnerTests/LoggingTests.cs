using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Assurance.UnitTests.TestModels;
using AwesomeAssertions;
using Kekiri.Xunit;
using Spiffy.Monitoring;

namespace Assurance.UnitTests.RunnerTests;

public class LoggingTests : Scenarios<TestingContext>
{
    EventContext _myContext = new();
    ModifiedLogStrategy _myLogStrategy;
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
    public void Supplying_both_an_event_context_and_a_log_strategy_is_rejected()
    {
        Given(a_custom_log_strategy);
        WhenAsync(both_an_event_context_and_a_log_strategy_are_supplied).Throws();
        Then(the_conflicting_arguments_are_reported);
    }

    [Scenario]
    public void Reusing_a_log_strategy_across_runs_is_rejected()
    {
        Given(a_custom_log_strategy);
        WhenAsync(the_same_log_strategy_is_used_twice).Throws();
        Then(the_single_use_violation_is_reported);
    }

    [Scenario]
    public void A_log_strategy_without_an_event_context_is_rejected()
    {
        WhenAsync(a_strategy_with_no_event_context_is_supplied).Throws();
        Then(the_missing_event_context_is_reported);
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

    async Task using_my_event_context()
    {
        Context.Result = await Runner.RunInParallel(
            "TheTaskName",
            Context.Existing,
            Context.Replacement,
             _myContext);
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
            "RunTests",
            () => new List<string> { "1", "2", "3" },
            () => new List<string> { "1", "2", "3" },
            logStrategy: _myLogStrategy);
    }

    async Task different_list_results_are_compared()
    {
        _listResult = await Runner.RunInParallel(
            "RunTests",
            () => new List<string> { "1", "2", "3", "5", "4" },
            () => new List<string> { "1", "2", "3", "4", "5" },
            logStrategy: _myLogStrategy);
    }

    async Task both_an_event_context_and_a_log_strategy_are_supplied()
    {
        _listResult = await Runner.RunInParallel(
            "RunTests",
            () => new List<string> { "1" },
            () => new List<string> { "1" },
            eventContext: _myContext,
            logStrategy: _myLogStrategy);
    }

    async Task the_same_log_strategy_is_used_twice()
    {
        await same_list_results_are_compared();
        await same_list_results_are_compared();
    }

    async Task a_strategy_with_no_event_context_is_supplied()
    {
        Context.Result = await Runner.RunInParallel(
            "RunTests",
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
        Context.Result.EventContext["Differences"].ToString().Should().ContainAll("(1 differences)", "Values (foo,doo)");
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
        _listResult.EventContext.Operation.Should().Be("RunTests");
    }

    void the_conflicting_arguments_are_reported()
    {
        Catch<ArgumentException>()
            .Message.Should().Contain("not both");
    }

    void the_single_use_violation_is_reported()
    {
        Catch<InvalidOperationException>()
            .Message.Should().Contain("single-use");
    }

    void the_missing_event_context_is_reported()
    {
        Catch<ArgumentException>()
            .Message.Should().Contain(nameof(NullContextLogStrategy));
    }
}
