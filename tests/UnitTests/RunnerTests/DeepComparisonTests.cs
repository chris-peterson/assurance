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
        result.ResultComparison.Differences.Should().Contain("Name");
        result.ResultComparison.Differences.Should().Contain("Age");
        result.ResultComparison.Differences.Should().Contain("Street");
        result.ResultComparison.Differences.Should().Contain("City");
    }

    [Fact]
    public async Task Nested_object_difference_is_detected()
    {
        var result = await Runner.RunInParallel(
            "NestedDiffTest",
            () => new Person { Name = "Alice", Age = 30, Address = new Address { Street = "123 Main", City = "Springfield" } },
            () => new Person { Name = "Alice", Age = 30, Address = new Address { Street = "123 Main", City = "Shelbyville" } });

        result.ResultComparison.AreEqual.Should().BeFalse();
        result.ResultComparison.Differences.Should().Contain("City");
        result.ResultComparison.Differences.Should().NotContain("Name");
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
}
