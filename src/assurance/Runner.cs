using System;
using System.Threading.Tasks;
using Assurance.Compare;
using Assurance.Logging;
using Spiffy.Monitoring;

namespace Assurance;

public static class Runner
{
    public static async Task<RunResult<T>> RunInParallel<T>(
        string taskName,
        Func<T> existing,
        Func<T> replacement,
        EventContext eventContext = null,
        IComparisonStrategy<T> comparisonStrategy = null)
    {
        return await RunInParallel(
            taskName,
            existing,
            replacement,
            new DefaultLogStrategy<T>(eventContext),
            comparisonStrategy);
    }

    public static async Task<RunResult<T>> RunInParallel<T>(
        string taskName,
        Func<T> existing,
        Func<T> replacement,
        ILogStrategy<T> logStrategy,
        IComparisonStrategy<T> comparisonStrategy = null)
    {
        // Task.Run, not Task.FromResult: the latter invokes the delegate on the calling thread as
        // the wrapper is awaited, so the two implementations would run one after the other
        return await RunInParallel(
            taskName,
            existing != null ? (Func<Task<T>>)(() => Task.Run(existing)) : null,
            replacement != null ? (Func<Task<T>>)(() => Task.Run(replacement)) : null,
            logStrategy,
            comparisonStrategy);
    }

    public static async Task<RunResult<T>> RunInParallel<T>(
        string taskName,
        Func<Task<T>> existing,
        Func<Task<T>> replacement,
        EventContext eventContext = null,
        IComparisonStrategy<T> comparisonStrategy = null)
    {
        return await RunInParallel(
            taskName,
            existing,
            replacement,
            new DefaultLogStrategy<T>(eventContext),
            comparisonStrategy);
    }

    public static async Task<RunResult<T>> RunInParallel<T>(
        string taskName,
        Func<Task<T>> existing,
        Func<Task<T>> replacement,
        ILogStrategy<T> logStrategy,
        IComparisonStrategy<T> comparisonStrategy = null)
    {
        var logRun = logStrategy.Begin(taskName);

        // both implementations are timed against one context, so the run needs one even
        // when the log writes somewhere else entirely
        var timingContext = logRun.EventContext;
        var isMyTimingContext = timingContext == null;
        if (isMyTimingContext)
        {
            timingContext = new EventContext("Assurance", taskName);
            timingContext.AppendToValue("Warnings",
                $"{logRun.GetType().Name} supplied no {nameof(EventContext)}; timings are recorded here",
                ",");
        }

        void Warn(string message)
        {
            logRun.AppendToValue("Warnings", message);
            if (isMyTimingContext)
            {
                timingContext.AppendToValue("Warnings", message, ",");
            }
        }

        if (existing == null)
        {
            Warn("Existing implementation is undefined");
            existing = () => Task.FromResult(default(T));
        }
        if (replacement == null)
        {
            Warn("Replacement implementation is undefined");
            replacement = () => Task.FromResult(default(T));
        }

        try
        {
            var existingTask = new AsyncTaskRunner<T>(timingContext, "Existing", existing, true);
            var replacementTask = new AsyncTaskRunner<T>(timingContext, "Replacement", replacement, false);

            await Task.WhenAll(existingTask.RunAsync(), replacementTask.RunAsync());

            comparisonStrategy ??= new DeepComparisonStrategy<T>();
            var result = new RunResult<T>(existingTask.Result, replacementTask.Result, comparisonStrategy, logRun);
            logRun.LogRunResult(result);

            return result;
        }
        catch
        {
            // no RunResult reaches the caller, so this is the only place left to close out the run
            logRun.Complete();
            throw;
        }
        finally
        {
            if (isMyTimingContext)
            {
                timingContext.Dispose();
            }
        }
    }

    class AsyncTaskRunner<T>
    {
        readonly EventContext _context;
        readonly string _label;

        public AsyncTaskRunner(EventContext context, string label, Func<Task<T>> work, bool shouldRethrowExceptions)
        {
            _context = context;
            _label = label;
            Work = work;
            ShouldRethrowExceptions = shouldRethrowExceptions;
        }

        public async Task<T> RunAsync()
        {
            using (_context.Timers.TimeOnce(_label))
            {
                try
                {
                    Result = await Work.Invoke();
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

        Func<Task<T>> Work { get; }
        bool ShouldRethrowExceptions { get; }
    }
}
