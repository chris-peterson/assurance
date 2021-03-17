using System;
using System.Threading.Tasks;

namespace Assurance
{
    public class RunResult<T>
    {
        public RunResult(T existing, T replacement)
        {
            Existing = existing;
            Replacement = replacement;
        }

        public T Existing { get; }
        public T Replacement { get; }
        public bool SameResult => Existing.Equals(Replacement);
        public T UseExisting() => Existing;
        public T UseReplacement() => Replacement;
    }
    public static class Runner
    {
        public static async Task<RunResult<T>> Run<T>(Func<Task<T>> existing, Func<Task<T>> replacement)
        {
            var existingTask = existing();
            var replacementTask = replacement();
            await Task.WhenAll(existingTask, replacementTask);

            return new RunResult<T>(existingTask.Result, replacementTask.Result);
        }
    }
}
