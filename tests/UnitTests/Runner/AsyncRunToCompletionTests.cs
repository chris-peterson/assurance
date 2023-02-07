using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Kekiri.Xunit;

namespace Assurance.UnitTests
{
    public class AsyncRunToCompletionTests : Scenarios<AsyncRunnerTestContext>
    {
        [Scenario]
        public void UseExisting()
        {
            Given(different_results);
            WhenAsync(implementations_are_run);
            ThenAsync(existing_result_is_used);
        }

        [Scenario]
        public void UseReplacement()
        {
            Given(different_results);
            WhenAsync(implementations_are_run);
            ThenAsync(replacement_result_is_used);
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

        const string TestString = "foo";
        void implementations_have_same_result()
        {
            Context.Existing = async () =>
            {
                await Task.Delay(400);
                return TestString;
            };
            Context.Replacement = async () =>
            {
                await Task.Delay(500);
                return TestString;
            };
        }

        void different_results()
        {
            Context.Existing = () => Task.FromResult(TestString);
            Context.Replacement = () => Task.FromResult(TestString + "something else");
        }

        async Task implementations_are_run()
        {
            Context.Result = await Runner.RunInParallel(
                "RunTests",
                Context.Existing,
                Context.Replacement);
        }

        void the_results_are_identical()
        {
            Context.Result.SameResult.Should().BeTrue();
            Context.Result.Existing.Should().BeSameAs(Context.Result.Replacement);
            Context.Result.Existing.Should().BeSameAs(TestString);
            Context.Result.Replacement.Should().BeSameAs(TestString);
        }

        async Task existing_result_is_used()
        {
            var result = Context.Result.UseExisting();
            result.Should().BeSameAs(await Context.Existing.Invoke());
            result.Should().NotBe(await Context.Replacement.Invoke());
            Context.Result.SameResult.Should().BeFalse();
        }

        async Task replacement_result_is_used()
        {
            var result = Context.Result.UseReplacement();
            result.Should().BeSameAs(await Context.Replacement.Invoke());
            result.Should().NotBe(await Context.Existing.Invoke());
            Context.Result.SameResult.Should().BeFalse();
        }

        void the_result_is_null()
        {
            Context.Result.Existing.Should().BeNull();
            Context.Result.Replacement.Should().BeNull();
            Context.Result.SameResult.Should().BeTrue();
        }
    }
}
