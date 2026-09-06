using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assurance.Compare;
using AwesomeAssertions;
using Xunit;

namespace Assurance.UnitTests.RunnerTests;

public class DeepComparisonTests
{
    class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public Address Address { get; set; }
    }

    class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
    }

    class Holder
    {
        public object Value { get; set; }
    }

    [Fact]
    public async Task Same_complex_objects_are_equal()
    {
        var result = await Runner.RunInParallel(
            "DeepEqualTest",
            () => new Person { Name = "Alice", Age = 30, Address = new Address { Street = "123 Main", City = "Springfield" } },
            () => new Person { Name = "Alice", Age = 30, Address = new Address { Street = "123 Main", City = "Springfield" } });

        result.ResultComparison.AreEqual.Should().BeTrue();
    }

    [Fact]
    public async Task Different_complex_objects_are_not_equal()
    {
        var result = await Runner.RunInParallel(
            "DeepDiffTest",
            () => new Person { Name = "Alice", Age = 30, Address = new Address { Street = "123 Main", City = "Springfield" } },
            () => new Person { Name = "Bob", Age = 25, Address = new Address { Street = "456 Oak", City = "Shelbyville" } });

        result.ResultComparison.AreEqual.Should().BeFalse();
        result.ResultComparison.Differences.Should().HaveCount(4);
        result.ResultComparison.Differences.ToString().Should().ContainAll("Name", "Age", "Street", "City");
    }

    [Fact]
    public async Task Nested_object_difference_is_detected()
    {
        var result = await Runner.RunInParallel(
            "NestedDiffTest",
            () => new Person { Name = "Alice", Age = 30, Address = new Address { Street = "123 Main", City = "Springfield" } },
            () => new Person { Name = "Alice", Age = 30, Address = new Address { Street = "123 Main", City = "Shelbyville" } });

        result.ResultComparison.AreEqual.Should().BeFalse();
        result.ResultComparison.Differences.ToString().Should().Contain("City");
        result.ResultComparison.Differences.ToString().Should().NotContain("Name");
    }

    [Fact]
    public async Task Differences_are_logged()
    {
        var result = await Runner.RunInParallel(
            "LogDiffTest",
            () => new Person { Name = "Alice", Age = 30 },
            () => new Person { Name = "Bob", Age = 30 });

        result.ResultComparison.AreEqual.Should().BeFalse();
        result.EventContext["Differences"].Should().NotBeNull();
    }

    [Fact]
    public async Task Every_difference_is_its_own_entry()
    {
        var result = await Runner.RunInParallel(
            "MultipleDiffLogTest",
            () => new Person { Name = "Alice", Age = 30, Address = new Address { Street = "123 Main", City = "Springfield" } },
            () => new Person { Name = "Bob", Age = 25, Address = new Address { Street = "456 Oak", City = "Shelbyville" } });

        var differences = result.EventContext["Differences"].ToString();
        differences.Split("; ").Should().BeEquivalentTo(
            "Name: Alice != Bob",
            "Age: 30 != 25",
            "Address.Street: 123 Main != 456 Oak",
            "Address.City: Springfield != Shelbyville");
    }

    [Fact]
    public async Task A_log_can_take_the_first_difference_alone()
    {
        var result = await Runner.RunInParallel(
            "FirstDifferenceTest",
            () => new Person { Name = "Alice", Age = 30, Address = new Address { City = "Springfield" } },
            () => new Person { Name = "Bob", Age = 25, Address = new Address { City = "Shelbyville" } });

        var differences = result.ResultComparison.Differences;
        differences.Count.Should().Be(3);
        differences[0].Should().Be("Name: Alice != Bob");
    }

    [Fact]
    public async Task A_type_difference_is_reported_as_the_two_type_names()
    {
        var result = await Runner.RunInParallel(
            "TypeDiffTest",
            () => new Holder { Value = 1 },
            () => new Holder { Value = "1" });

        result.ResultComparison.AreEqual.Should().BeFalse();
        result.ResultComparison.Differences.Should().Equal("Value: System.Int32 != System.String");
    }

    [Fact]
    public async Task A_line_break_in_a_value_is_escaped()
    {
        var result = await Runner.RunInParallel(
            "LineBreakDiffTest",
            () => new Person { Name = "one\r\ntwo" },
            () => new Person { Name = "one two" });

        var differences = result.EventContext["Differences"].ToString();
        // an event is read a line at a time, so a value carrying a break must not split it
        differences.Should().NotContainAny("\r", "\n");
        differences.Should().Be(@"Name: one\r\ntwo != one two");
    }

    [Fact]
    public async Task A_literal_backslash_stays_distinguishable_from_an_escaped_break()
    {
        var result = await Runner.RunInParallel(
            "BackslashDiffTest",
            () => new Person { Name = @"one\ntwo" },
            () => new Person { Name = "one\ntwo" });

        result.ResultComparison.Differences.Should().Equal(@"Name: one\\ntwo != one\ntwo");
    }

    [Fact]
    public async Task A_line_break_in_a_property_name_is_escaped()
    {
        var result = await Runner.RunInParallel(
            "PropertyNameBreakDiffTest",
            () => new Dictionary<string, string> { ["k\nLevel=Error"] = "1" },
            () => new Dictionary<string, string> { ["k\nLevel=Error"] = "2" });

        var differences = result.EventContext["Differences"].ToString();
        // a dictionary key reaches the entry as its property name, so it splits the event too
        differences.Should().NotContainAny("\r", "\n");
        differences.Should().Contain(@"k\nLevel=Error");
    }

    [Fact]
    public async Task A_quote_in_a_value_is_passed_through_unaltered()
    {
        var result = await Runner.RunInParallel(
            "QuoteDiffTest",
            () => new Person { Name = "a\"b'c`d" },
            () => new Person { Name = "z" });

        // encapsulation is Spiffy's, and pre-escaping the quote corrupts the value under a
        // formatter that escapes quotes itself
        result.ResultComparison.Differences.Should().Equal("Name: a\"b'c`d != z");
    }

    [Fact]
    public async Task A_null_value_is_distinguishable_from_an_empty_one()
    {
        var result = await Runner.RunInParallel(
            "NullValueDiffTest",
            () => new Person { Name = null },
            () => new Person { Name = "" });

        result.ResultComparison.Differences.Should().Equal("Name: (null) != ");
    }

    [Fact]
    public async Task Differences_stop_at_the_cap()
    {
        var existing = Enumerable.Range(0, 150).Select(i => i.ToString()).ToList();
        var replacement = Enumerable.Range(1000, 150).Select(i => i.ToString()).ToList();

        var result = await Runner.RunInParallel(
            "CappedDiffTest",
            () => existing,
            () => replacement);

        result.ResultComparison.AreEqual.Should().BeFalse();
        result.ResultComparison.Differences.Count
            .Should().Be(DeepComparisonStrategy<object>.DefaultMaxDifferences);
    }

    [Fact]
    public async Task The_cap_can_be_raised()
    {
        var existing = Enumerable.Range(0, 150).Select(i => i.ToString()).ToList();
        var replacement = Enumerable.Range(1000, 150).Select(i => i.ToString()).ToList();

        var result = await Runner.RunInParallel(
            "RaisedCapDiffTest",
            () => existing,
            () => replacement,
            comparisonStrategy: new DeepComparisonStrategy<List<string>>(maxDifferences: 200));

        result.ResultComparison.Differences.Count.Should().Be(150);
    }

    [Fact]
    public void An_empty_difference_is_not_a_difference()
    {
        // the shape a pre-2.0 strategy used to report equality, which params would otherwise turn
        // into a phantom entry on an equal result
        var comparison = new ResultComparison(true, "");

        comparison.Differences.Should().BeEmpty();
    }

    [Fact]
    public void A_null_difference_is_not_a_difference()
    {
        var comparison = new ResultComparison(false, null, "Name: Alice != Bob");

        comparison.Differences.Should().Equal("Name: Alice != Bob");
    }

    [Fact]
    public void A_comparison_must_be_allowed_at_least_one_difference()
    {
        // zero would report every comparison as equal, which is the one answer the library
        // must never invent
        var tooLow = () => new DeepComparisonStrategy<Person>(maxDifferences: 0);

        tooLow.Should().Throw<ArgumentOutOfRangeException>();
    }
}
