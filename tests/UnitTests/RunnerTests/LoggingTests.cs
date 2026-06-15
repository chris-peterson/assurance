using Assurance.UnitTests.TestModels;
using AwesomeAssertions;
using Kekiri.Xunit;
using Spiffy.Monitoring;
using System;
using System.Collections.Generic;

namespace Assurance.UnitTests.RunnerTests;

public class LoggingTests : Scenarios<TestingContext>
{
    private EventContext _myContext = new();
    private ModifiedLogStrategy _mylogStrategy = null;
    private RunResult<List<string>> _listResult;

    [Scenario]
    public void Use_own_event_context()
    {
        When(using_my_event_context);
        Then(logged_fields_are_namespaced);
    }

    [Scenario]
    public void Use_generated_event_context()
    {
        When(event_context_not_specified);
        Then(logged_fields_are_NOT_namespaced);
    }

    [Scenario]
    public void Default_logging_outputs_existing_use_when_chosen()
    {
        When(existing_use_is_chosen_after_a_comparison);
        Then(default_existing_use_is_logged);
    }

    [Scenario]
    public void Default_logging_outputs_replacement_use_when_chosen()
    {
        When(replacement_use_is_chosen_after_a_comparison);
        Then(default_replacement_use_is_logged);
    }

    [Scenario]
    public void Include_default_same_output_if_none_is_provided()
    {
        When(same_list_results_are_compared);
        Then(default_same_output_is_logged);
    }

    [Scenario]
    public void Include_default_difference_output_if_none_is_provided()
    {
        When(different_list_results_are_compared);
        Then(default_difference_output_is_logged);
    }

    [Scenario]
    public void Include_custom_same_output()
    {
        Given(a_custom_log_strategy);
        When(same_list_results_are_compared);
        Then(custom_same_output_is_logged);
    }

    [Scenario]
    public void Include_custom_difference_output()
    {
        Given(a_custom_log_strategy);
        When(different_list_results_are_compared);
        Then(Custom_difference_output_is_logged);
    }

    private void a_custom_log_strategy()
    {
        _mylogStrategy = new ModifiedLogStrategy("ListTest");
    }

    async void using_my_event_context()
    {
        Context.Result = await Runner.RunInParallel(
            "TheTaskName",
            Context.Existing,
            Context.Replacement,
             _myContext);
    }

    async void event_context_not_specified()
    {
        Context.Result = await Runner.RunInParallel(
            "TheTaskName",
            Context.Existing,
            Context.Replacement);
    }

    private async void same_list_results_are_compared()
    {
        _listResult = await Runner.RunInParallel(
            "RunTests",
            () => new List<string> { "1", "2", "3" },
            () => new List<string> { "1", "2", "3" },
            logStrategy: _mylogStrategy);
    }

    private async void different_list_results_are_compared()
    {
        _listResult = await Runner.RunInParallel(
            "RunTests",
            () => new List<string> { "1", "2", "3", "5", "4" },
            () => new List<string> { "1", "2", "3", "4", "5" },
            logStrategy: _mylogStrategy);
    }

    private void existing_use_is_chosen_after_a_comparison()
    {
        event_context_not_specified();
        Context.Result.UseExisting();
    }

    private void replacement_use_is_chosen_after_a_comparison()
    {
        event_context_not_specified();
        Context.Result.UseReplacement();
    }

    void logged_fields_are_namespaced()
    {
        _myContext.Component.Should().NotBe("Assurance");
        _myContext.Operation.Should().NotBe("TheTaskName");
        _myContext["AssuranceTask"] = "LoggingTests";
        _myContext["AssuranceResult"].Should().Be("same");
    }
    
    void logged_fields_are_NOT_namespaced()
    {
        Context.Result.EventContext.Component.Should().Be("Assurance");
        Context.Result.EventContext.Operation.Should().Be("TheTaskName");
        Context.Result.EventContext.Contains("Task").Should().BeFalse();
        Context.Result.EventContext["Result"].Should().Be("same");
    }

    private void default_existing_use_is_logged()
    {
        Context.Result.EventContext["Use"].Should().Be("existing");
    }

    private void default_replacement_use_is_logged()
    {
        Context.Result.EventContext["Use"].Should().Be("replacement");
    }

    private void default_same_output_is_logged()
    {
        _listResult.EventContext["Result"].Should().Be("same");
        _listResult.EventContext["Differences"].Should().Be(string.Empty);
    }

    private void default_difference_output_is_logged()
    {
        _listResult.EventContext["Result"].Should().Be("different");
        _listResult.EventContext["Differences"].Should().Be(_listResult.ResultComparison.Differences);
    }

    private void custom_same_output_is_logged()
    {
        _listResult.EventContext["Result"].Should().Be("equal");
        _listResult.EventContext["Differences"].Should().Be(string.Empty);
        _listResult.EventContext["ExistingFive"].Should().Be(string.Empty);
        _listResult.EventContext["ReplacementFive"].Should().Be(string.Empty);
    }

    private void Custom_difference_output_is_logged()
    {
        _listResult.EventContext["Result"].Should().Be("notEqual");
        _listResult.EventContext["Differences"].Should().Be(string.Empty);
        _listResult.EventContext["ExistingFive"].Should().Be("1,2,3,5,4");
        _listResult.EventContext["ReplacementFive"].Should().Be("1,2,3,4,5");
    }

}
