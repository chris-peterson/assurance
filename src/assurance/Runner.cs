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
            var context = new EventContext("Assurance", taskName);

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

            var existingTask = new TaskRunner<T>(context, "Existing", existing, true);
            var replacementTask = new TaskRunner<T>(context, "Replacement", replacement, false);

            await Task.WhenAll(existingTask.RunAsync(), replacementTask.RunAsync());

            var result = new RunResult<T>(existingTask.Result, replacementTask.Result, context);
            if (result.SameResult)
            {
                context["Result"] = "same";
            }
            else
            {
                context["Result"] = "different";
                context["Existing"] = existingTask.Result;
                context["Replacement"] = replacementTask.Result;
            }

            return result;
        }

        class TaskRunner<T>
        {
            readonly EventContext _context;
            readonly string _label;

            public TaskRunner(EventContext context, string label, Func<T> work, bool shouldRethrowExceptions)
            {
                _context = context;
                _label = label;
                Work = new Task<T>(work);
                ShouldRethrowExceptions = shouldRethrowExceptions;
            }

            public async Task<T> RunAsync()
            {
                using (_context.Timers.TimeOnce(_label))
                {
                    Work.Start();
                    try
                    {
                        Result = await Work;
                    }
                    catch (Exception ex)
                    {
                        Exception = ex;
                        _context.IncludeException(Exception, _label);
                        if (ShouldRethrowExceptions)
                        {
                            throw;
                        }
                        else
                        {
                            _context.SetToInfo();
                        }
                    }

                    return Result;
                }
            }

            public T Result { get; private set; }
            public Exception Exception { get; private set; }

            Task<T> Work { get; }
            bool ShouldRethrowExceptions { get; }
        }
    }
}
