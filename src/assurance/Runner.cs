using System;
using System.Threading.Tasks;
using Assurance.Compare;
using Assurance.Logging;
using Spiffy.Monitoring;

namespace Assurance;

public static class Runner
{
    /// <summary>
    /// Runs both implementations, logs how their results compared, and hands back a result the
    /// caller picks a side from.
    /// </summary>
    /// <param name="taskName">Names the run in the log.</param>
    /// <param name="existing">The implementation in use today. Its exceptions reach the caller.</param>
    /// <param name="replacement">
    /// The implementation being introduced. Its exceptions are logged and swallowed, so it cannot
    /// break a caller that is still using <paramref name="existing"/>.
    /// </param>
    /// <param name="logStrategy">
    /// What the run logs, or null for <see cref="DefaultLogStrategy{T}"/>. To record into your own
    /// event, pass <c>new DefaultLogStrategy&lt;T&gt;(eventContext)</c>.
    /// </param>
    /// <param name="comparisonStrategy">
    /// How the two results are compared, or null for <see cref="DeepComparisonStrategy{T}"/>.
    /// </param>
    /// <remarks>
    /// Both delegates are dispatched to the thread pool and run concurrently, so anything they
    /// share has to be safe to touch from two threads, and thread-affine state on the calling
    /// thread does not reach them.
    /// </remarks>
    public static async Task<RunResult<T>> RunInParallel<T>(
        string taskName,
        Func<T> existing,
        Func<T> replacement,
        ILogStrategy<T> logStrategy = null,
        IComparisonStrategy<T> comparisonStrategy = null)
    {
        // Task.Run, not Task.FromResult: the latter invokes the delegate on the calling thread as
        // the wrapper is awaited, so the two implementations would run one after the other
        return await RunInParallel(
            taskName,
            existing != null ? (Func<Task<T>>)(() => Task.Run(existing)) : null,
            replacement != null ? (Func<Task<T>>)(() => Task.Run(replacement)) : null,
            logStrategy,
            comparisonStrategy).ConfigureAwait(false);
    }

    /// <inheritdoc cref="RunInParallel{T}(string, Func{T}, Func{T}, ILogStrategy{T}, IComparisonStrategy{T})"/>
    public static async Task<RunResult<T>> RunInParallel<T>(
        string taskName,
        Func<Task<T>> existing,
        Func<Task<T>> replacement,
        ILogStrategy<T> logStrategy = null,
        IComparisonStrategy<T> comparisonStrategy = null)
    {
        logStrategy ??= new DefaultLogStrategy<T>();

        var logRun = logStrategy.Begin(taskName);
        if (logRun == null)
        {
            throw new InvalidOperationException(
                $"{logStrategy.GetType().Name}.{nameof(ILogStrategy<T>.Begin)} returned null");
        }

        EventContext timingContext = null;
        bool isMyTimingContext = false;
        try
        {
            // both implementations are timed against one context, so the run needs one even
            // when the log writes somewhere else entirely
            timingContext = logRun.EventContext;
            if (timingContext == null)
            {
                timingContext = new EventContext(AssuranceLog.Component, taskName);
                // only once the context exists, so the finally below has something to dispose
                isMyTimingContext = true;
                timingContext.AppendToValue("Warnings",
                    $"{logRun.GetType().Name} supplied no {nameof(EventContext)}; timings are recorded here",
                    ",");
            }

            var levelBeforeRun = timingContext.Level;

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

            var existingTask = new AsyncTaskRunner<T>(timingContext, "Existing", existing, true);
            var replacementTask = new AsyncTaskRunner<T>(timingContext, "Replacement", replacement, false);

            await Task.WhenAll(existingTask.RunAsync(), replacementTask.RunAsync()).ConfigureAwait(false);

            // reaching here means the existing implementation succeeded, so a replacement failure is
            // not the caller's problem and the level IncludeException raised comes back down. The
            // level it comes back down to is the caller's, who may have raised it themselves before
            // handing the context over. Done here rather than inside the task, whose completion
            // order is no longer fixed, so the level does not depend on which side finished last.
            if (replacementTask.Exception != null)
            {
                timingContext.SetLevel(levelBeforeRun);
            }

            comparisonStrategy ??= new DeepComparisonStrategy<T>();
            var result = new RunResult<T>(existingTask.Result, replacementTask.Result, comparisonStrategy, logRun);
            logRun.LogRunResult(result);

            return result;
        }
        catch
        {
            // no RunResult reaches the caller, so this is the only place left to close out the run
            try
            {
                logRun.Complete();
            }
            catch
            {
                // ILogRun is caller-supplied, and the exception on its way out is the one the
                // caller needs to see
            }
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
                    Result = await Work.Invoke().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Exception = ex;
                    _context.IncludeException(Exception, _label);
                    if (ShouldRethrowExceptions)
                    {
                        throw;
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
