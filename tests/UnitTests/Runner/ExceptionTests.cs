using System;
using System.Threading.Tasks;
using FluentAssertions;
using Kekiri.Xunit;

namespace Assurance.UnitTests
{
    public class ExceptionTests : Scenarios<RunnerTestContext>
    {
        [Scenario]
        public void ExistingThrows()
        {
            Given(existing_throws);
            WhenAsync(implementations_are_run).Throws();
            Then(an_exception_is_raised);
        }

        [Scenario]
        public void ReplacementThrows()
        {
            Given(replacement_throws)
                .But(existing_succeeds);
            WhenAsync(implementations_are_run);
            Then(existing_result_is_used);
        }

        [Scenario]
        public void BothThrow()
        {
            Given(existing_throws)
                .And(replacement_throws);
            WhenAsync(implementations_are_run).Throws();
            Then(an_exception_is_raised);
        }

        void existing_throws()
        {
            Context.Existing = () => throw new Exception("from existing");
        }

        void existing_succeeds()
        {
            Context.Existing = () => "foo";
        }

        void replacement_throws()
        {
            Context.Replacement = () => throw new Exception("from replacement");
        }

        async Task implementations_are_run()
        {
            Context.Result = await Runner.RunInParallel(
                "ExceptionTests",
                Context.Existing,
                Context.Replacement);
        }

        void an_exception_is_raised()
        {
            Catch<Exception>()
                .Message.Should().Be("from existing");
        }

        void existing_result_is_used()
        {
            Context.Result.Existing.Should().Be("foo");
            Context.Result.Replacement.Should().BeNull();
            Context.Result.SameResult.Should().BeFalse();
        }
    }
}
