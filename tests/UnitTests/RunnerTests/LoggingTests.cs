using FluentAssertions;
using Kekiri.Xunit;
using Spiffy.Monitoring;

namespace Assurance.UnitTests.RunnerTests;

public class LoggingTests : Scenarios<TestingContext>
{
    private EventContext _myContext = new();

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

    async void using_my_event_context()
    {
        Context.Result = await Runner.RunInParallel(
            "TheTaskName",
            Context.Existing,
            Context.Replacement,
             _myContext);
    }
    
    void logged_fields_are_namespaced()
    {
        _myContext.Component.Should().NotBe("Assurance");
        _myContext.Operation.Should().NotBe("TheTaskName");
        _myContext["AssuranceTask"] = "LoggingTests";
        _myContext["AssuranceResult"].Should().Be("same");
    }
    
    async void event_context_not_specified()
    {
        Context.Result = await Runner.RunInParallel(
            "TheTaskName",
            Context.Existing,
            Context.Replacement);
    }

    void logged_fields_are_NOT_namespaced()
    {
        Context.Result.EventContext.Component.Should().Be("Assurance");
        Context.Result.EventContext.Operation.Should().Be("TheTaskName");
        Context.Result.EventContext.Contains("Task").Should().BeFalse();
        Context.Result.EventContext["Result"].Should().Be("same");
    }
}
