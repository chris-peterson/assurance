using System;
using System.Threading.Tasks;
using Spiffy.Monitoring;

namespace Assurance
{
    public static class Runner
    {
        public static async Task<RunResult<T>> RunInParallel<T>(
            string taskName,
            Func<T> existing, Func<T> replacement)
        {
            using var context = new EventContext("Assurance", taskName);

            if (existing == null)
            {
                context.AppendToValue("Warnings", "Existing implementation is undefined", ",");
                existing = () => default;
            }
            if (replacement == null)
            {
                context.AppendToValue("Warnings", "Replacement implementation is undefined", ",");
                replacement = () => default;
            }

            var existingTask = new TaskRunner<T>(existing, true);
            var replacementTask = new TaskRunner<T>(replacement, false);

            await Task.WhenAll(existingTask.RunAsync(), replacementTask.RunAsync());

            if (existingTask.Result == null)
            {
                context["Result"] = "null from existing";
            }
            else if (existingTask.Result.Equals(replacementTask.Result))
            {
                context["Result"] = "same";
            }
            else
            {
                context["Result"] = "different";
                context["Existing"] = existingTask.Result;
                context["Replacement"] = replacementTask.Result;
            }

            return new RunResult<T>(existingTask.Result, replacementTask.Result);
        }

        class TaskRunner<T>
        {
            public TaskRunner(Func<T> work, bool shouldRethrowExceptions)
            {
                Work = Task.Factory.StartNew(work);
                ShouldRethrowExceptions = shouldRethrowExceptions;
            }

            public async Task<T> RunAsync()
            {
                try
                {
                    Result = await Work;
                }
                catch (Exception ex)
                {
                    Exception = ex;
                    if (ShouldRethrowExceptions)
                    {
                        throw;
                    }
                }

                return Work.IsCompletedSuccessfully ? Result : default;
            }

            public T Result { get; private set; }
            public Exception Exception { get; private set; }

            Task<T> Work { get; }
            bool ShouldRethrowExceptions { get; }
        }
    }
}
