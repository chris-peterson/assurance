using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Kekiri.Xunit;

namespace Assurance.UnitTests
{
    public class RunToCompletionTests : Scenarios<RunnerTestContext>
    {
        [Scenario]
        public void UseExisting()
        {
            Given(different_results);
            WhenAsync(implementations_are_run);
            Then(existing_result_is_used);
        }

        [Scenario]
        public void UseReplacement()
        {
            Given(different_results);
            WhenAsync(implementations_are_run);
            Then(replacement_result_is_used);
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
            Context.Existing = () =>
            {
                Thread.Sleep(500);
                return TestString;
            };
            Context.Replacement = () =>
            {
                Thread.Sleep(500);
                return TestString;
            };
        }

        void different_results()
        {
            Context.Existing = () => TestString;
            Context.Replacement = () => TestString + "something else";
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

        void existing_result_is_used()
        {
            var result = Context.Result.UseExisting();
            result.Should().BeSameAs(Context.Existing());
            result.Should().NotBe(Context.Replacement());
            Context.Result.SameResult.Should().BeFalse();
        }

        void replacement_result_is_used()
        {
            var result = Context.Result.UseReplacement();
            result.Should().BeSameAs(Context.Replacement());
            result.Should().NotBe(Context.Existing());
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
