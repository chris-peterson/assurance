using System.Threading.Tasks;
using AwesomeAssertions;
using Kekiri.Xunit;

namespace Assurance.UnitTests.RunnerTests;

/// <summary>
/// Asserts on the rendered event rather than on <c>ResultComparison.Differences</c>. A compared
/// value only reaches a reader through Spiffy's formatter, so the intermediate string passing a
/// check says nothing about whether the emitted line holds together.
/// </summary>
public class LogLineTests : Scenarios<TestingContext>
{
    string _taskName;
    string _existing;

    [Scenario]
    public void ALineBreakInAValueDoesNotSplitTheEvent()
    {
        Given(a_value_carrying_a_line_break);
        WhenAsync(the_values_are_compared);
        Then(the_event_is_one_line);
    }

    [Scenario]
    public void SeveralDifferencesAreWrittenOnOneLine()
    {
        Given(two_values_that_differ_in_several_places);
        WhenAsync(the_values_are_compared);
        Then(the_event_is_one_line)
            .And(the_differences_are_separated_on_that_line);
    }

    [Scenario]
    public void QuotesAreLeftForSpiffyToEncapsulate()
    {
        Given(a_value_holding_every_quote_character);
        WhenAsync(the_values_are_compared);
        Then(the_value_reaches_the_field_unaltered);
    }

    void a_value_carrying_a_line_break()
    {
        _taskName = "LineBreakLogLine";
        _existing = "one\r\ntwo";
    }

    void two_values_that_differ_in_several_places()
    {
        _taskName = "SeveralDifferencesLogLine";
        _existing = "abc";
    }

    void a_value_holding_every_quote_character()
    {
        _taskName = "EveryQuoteLogLine";
        _existing = "a\"b'c`d";
    }

    async Task the_values_are_compared()
    {
        var existing = _existing;
        Context.Result = await Runner.RunInParallel(
            _taskName,
            () => existing,
            () => "z");
        Context.Result.UseExisting();
    }

    void the_event_is_one_line()
    {
        Context.EventFor(_taskName).Message.Should().NotContainAny("\r", "\n");
    }

    void the_differences_are_separated_on_that_line()
    {
        // Properties holds the value as it was rendered, encapsulation included
        Context.EventFor(_taskName).Properties["Differences"].Should().Be("\"abc != z\"");
    }

    void the_value_reaches_the_field_unaltered()
    {
        // Spiffy encapsulates by picking a quote the value does not hold, and this value holds all
        // three, so the inner quotes are rendered as they are. Escaping them here would not give it
        // an unused candidate, and would corrupt the value under a formatter that escapes quotes
        // itself, so the library passes the value through and leaves encapsulation to Spiffy.
        Context.EventFor(_taskName).Properties["Differences"].Should().Be("\"a\"b'c`d != z\"");
    }
}
