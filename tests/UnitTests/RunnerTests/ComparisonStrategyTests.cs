using System.Threading.Tasks;
using Assurance.Compare;
using AwesomeAssertions;
using Xunit;

namespace Assurance.UnitTests.RunnerTests;

public class ComparisonStrategyTests
{
    class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    [Fact]
    public async Task Default_strategy_uses_deep_comparison()
    {
        var result = await Runner.RunInParallel(
            "DefaultDeep",
            () => new Person { Name = "Alice", Age = 30 },
            () => new Person { Name = "Alice", Age = 30 });

        result.ResultComparison.AreEqual.Should().BeTrue();
    }

    [Fact]
    public async Task Explicit_deep_strategy()
    {
        var result = await Runner.RunInParallel(
            "ExplicitDeep",
            () => new Person { Name = "Alice", Age = 30 },
            () => new Person { Name = "Alice", Age = 30 },
            comparisonStrategy: new DeepComparisonStrategy<Person>());

        result.ResultComparison.AreEqual.Should().BeTrue();
    }

    [Fact]
    public async Task Shallow_equals_strategy_uses_equals()
    {
        var result = await Runner.RunInParallel(
            "ExplicitDefault",
            () => new Person { Name = "Alice", Age = 30 },
            () => new Person { Name = "Alice", Age = 30 },
            comparisonStrategy: new ShallowEqualsStrategy<Person>());

        result.ResultComparison.AreEqual.Should().BeFalse("Person does not override Equals, so different instances are not equal");
    }

    [Fact]
    public async Task Shallow_equals_strategy_same_reference()
    {
        var person = new Person { Name = "Alice", Age = 30 };

        var result = await Runner.RunInParallel(
            "ExplicitDefaultSameRef",
            () => person,
            () => person,
            comparisonStrategy: new ShallowEqualsStrategy<Person>());

        result.ResultComparison.AreEqual.Should().BeTrue();
    }

    [Fact]
    public async Task Custom_strategy_is_used()
    {
        var result = await Runner.RunInParallel(
            "CustomStrategy",
            () => new Person { Name = "Alice", Age = 30 },
            () => new Person { Name = "Bob", Age = 30 },
            comparisonStrategy: new NameOnlyComparisonStrategy());

        result.ResultComparison.AreEqual.Should().BeFalse();
        result.ResultComparison.Differences.ToString().Should().Contain("Alice");
        result.ResultComparison.Differences.ToString().Should().Contain("Bob");
    }

    [Fact]
    public async Task Custom_strategy_ignores_non_compared_fields()
    {
        var result = await Runner.RunInParallel(
            "CustomStrategyIgnore",
            () => new Person { Name = "Alice", Age = 30 },
            () => new Person { Name = "Alice", Age = 99 },
            comparisonStrategy: new NameOnlyComparisonStrategy());

        result.ResultComparison.AreEqual.Should().BeTrue("NameOnlyComparisonStrategy ignores Age");
    }

    class NameOnlyComparisonStrategy : IComparisonStrategy<Person>
    {
        public ResultComparison Compare(Person existing, Person replacement)
        {
            return existing.Name == replacement.Name
                ? new ResultComparison(true)
                : new ResultComparison(
                    false,
                    $"Names differ: {existing.Name} vs {replacement.Name}");
        }
    }
}
