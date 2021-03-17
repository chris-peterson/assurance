using System.Threading.Tasks;
using FluentAssertions;
using Kekiri.Xunit;

namespace Assurance.UnitTests
{
    public class ExecutorTestContext
    {
        public Task<string> TaskA { get; set; }
        public Task<string> TaskB { get; set; }
        public (string A, string B, bool AreSame) Result { get; set; }
    }

    public class ExecutorTests : Scenarios<ExecutorTestContext>
    {
        [Scenario]
        public async void CompareResults()
        {
            Given(Two_tasks_with_same_result);
            WhenAsync(Comparing_results);
            Then(The_results_are_identical);
        }

        const string TestString = "foo";
        void Two_tasks_with_same_result()
        {
            Context.TaskA = Task.FromResult(TestString);
            Context.TaskB = Task.FromResult(TestString);
        }
        async Task Comparing_results()
        {
            Context.Result = await Executor.CompareAsync(() => Context.TaskA, () => Context.TaskB);
        }

        void The_results_are_identical()
        {
            Context.Result.AreSame.Should().BeTrue();
            Context.Result.A.Should().BeSameAs(Context.Result.B);
            Context.Result.A.Should().BeSameAs(TestString);
        }
    }
}
