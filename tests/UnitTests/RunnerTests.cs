using System.Threading.Tasks;
using FluentAssertions;
using Kekiri.Xunit;

namespace Assurance.UnitTests
{
    public class RunnerTestContext
    {
        public Task<string> Existing { get; set; }
        public Task<string> Replacement { get; set; }
        public RunResult<string> Result { get; set; }
    }

    public class RunnerTests : Scenarios<RunnerTestContext>
    {
        [Scenario]
        public void SameResult()
        {
            Given(Same_result);
            WhenAsync(Implementations_are_run);
            Then(The_results_are_identical);
        }

        [Scenario]
        public void UseExisting()
        {
            Given(Different_results);
            WhenAsync(Implementations_are_run);
            Then(Existing_result_is_used);
        }

        [Scenario]
        public void UseReplacement()
        {
            Given(Different_results);
            WhenAsync(Implementations_are_run);
            Then(Replacement_result_is_used);
        }

        const string TestString = "foo";
        void Same_result()
        {
            Context.Existing = Task.FromResult(TestString);
            Context.Replacement = Task.FromResult(TestString);
        }

        void Different_results()
        {
            Context.Existing = Task.FromResult(TestString);
            Context.Replacement = Task.FromResult(TestString + "something different");
        }
        async Task Implementations_are_run()
        {
            Context.Result = await Runner.Run(
                () => Context.Existing,
                () => Context.Replacement);
        }

        void The_results_are_identical()
        {
            Context.Result.SameResult.Should().BeTrue();
            Context.Result.Existing.Should().BeSameAs(Context.Result.Replacement);
            Context.Result.Existing.Should().BeSameAs(TestString);
            Context.Result.Replacement.Should().BeSameAs(TestString);
        }


        void Existing_result_is_used()
        {
            var result = Context.Result.UseExisting();
            result.Should().BeSameAs(Context.Existing.Result);
            result.Should().NotBe(Context.Replacement.Result);
            Context.Result.SameResult.Should().BeFalse();
        }
        void Replacement_result_is_used()
        {
            var result = Context.Result.UseReplacement();
            result.Should().BeSameAs(Context.Replacement.Result);
            result.Should().NotBe(Context.Existing.Result);
            Context.Result.SameResult.Should().BeFalse();
        }
    }
}
