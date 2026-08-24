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
        IComparisonStrategy<T> comparisonStrategy = null,
        ILogStrategy<T> logStrategy = null)
    {
        return await RunInParallel(
            taskName,
            existing != null ? (Func<Task<T>>)(() => Task.FromResult(existing())) : null,
            replacement != null ? (Func<Task<T>>)(() => Task.FromResult(replacement())) : null,
            eventContext,
            comparisonStrategy,
            logStrategy);
    }

    public static async Task<RunResult<T>> RunInParallel<T>(
        string taskName,
        Func<Task<T>> existing,
        Func<Task<T>> replacement,
        EventContext eventContext = null,
        IComparisonStrategy<T> comparisonStrategy = null,
        ILogStrategy<T> logStrategy = null)
    {
        if (logStrategy != null && eventContext != null)
        {
            throw new ArgumentException(
                $"Supply either {nameof(eventContext)} or {nameof(logStrategy)}, not both -- " +
                "a log strategy owns the event context it writes to.", nameof(logStrategy));
        }

        logStrategy ??= new DefaultLogStrategy<T>(eventContext);
        logStrategy.Begin(taskName);

        if (logStrategy.EventContext == null)
        {
            throw new ArgumentException(
                $"{logStrategy.GetType().Name}.{nameof(ILogStrategy<T>.EventContext)} is null after " +
                $"{nameof(ILogStrategy<T>.Begin)}; timings for both implementations are recorded against it.",
                nameof(logStrategy));
        }

        if (existing == null)
        {
            logStrategy.AppendToValue("Warnings", "Existing implementation is undefined");
            existing = () => Task.FromResult(default(T));
        }
        if (replacement == null)
        {
            logStrategy.AppendToValue("Warnings", "Replacement implementation is undefined");
            replacement = () => Task.FromResult(default(T));
        }

        var existingTask = new AsyncTaskRunner<T>(logStrategy.EventContext, "Existing", existing, true);
        var replacementTask = new AsyncTaskRunner<T>(logStrategy.EventContext, "Replacement", replacement, false);

        await Task.WhenAll(existingTask.RunAsync(), replacementTask.RunAsync());

        comparisonStrategy ??= new DeepComparisonStrategy<T>();
        var result = new RunResult<T>(existingTask.Result, replacementTask.Result, comparisonStrategy, logStrategy);
        logStrategy.LogRunResult(result);
        
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
