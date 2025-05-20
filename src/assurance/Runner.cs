using System;
using System.Threading.Tasks;
using Spiffy.Monitoring;

namespace Assurance;

public static class Runner
{
    public static async Task<RunResult<T>> RunInParallel<T>(
        string taskName,
        Func<T> existing,
        Func<T> replacement,
        EventContext eventContext = null)
    {
        return await RunInParallel(
            taskName,
            existing != null ? (Func<Task<T>>)(() => Task.FromResult(existing())) : null,
            replacement != null ? (Func<Task<T>>)(() => Task.FromResult(replacement())) : null,
            eventContext);
    }

    public static async Task<RunResult<T>> RunInParallel<T>(
        string taskName,
        Func<Task<T>> existing,
        Func<Task<T>> replacement,
        EventContext eventContext = null)
    {
        string loggingPrefix = null;
        bool isMyEventContext = false;
        if (eventContext == null)
        {
            isMyEventContext = true;
            eventContext = new EventContext("Assurance", taskName);
        }
        else
        {
            loggingPrefix = "Assurance";
            eventContext[$"{loggingPrefix}Task"] = taskName;
        }
        var loggingContext = new LoggingContext(eventContext, loggingPrefix, isMyEventContext);

        if (existing == null)
        {
            loggingContext.AppendToValue("Warnings", "Existing implementation is undefined");
            existing = () => Task.FromResult(default(T));
        }
        if (replacement == null)
        {
            loggingContext.AppendToValue("Warnings", "Replacement implementation is undefined");
            replacement = () => Task.FromResult(default(T));
        }

        var existingTask = new AsyncTaskRunner<T>(eventContext, "Existing", existing, true);
        var replacementTask = new AsyncTaskRunner<T>(eventContext, "Replacement", replacement, false);

        await Task.WhenAll(existingTask.RunAsync(), replacementTask.RunAsync());

        var result = new RunResult<T>(existingTask.Result, replacementTask.Result, loggingContext);
        if (result.SameResult)
        {
            loggingContext.Log("Result", "same");
        }
        else
        {
            loggingContext.Log("Result", "different");
            loggingContext.Log("Existing", existingTask.Result);
            loggingContext.Log("Replacement", replacementTask.Result);
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
