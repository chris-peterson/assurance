# Overview

`Assurance` is a library to boost confidence when making code changes.

![image](https://user-images.githubusercontent.com/1980791/111552805-504fdd00-8740-11eb-8086-444e52abef65.png)

## Status

[![build](https://github.com/chris-peterson/assurance/actions/workflows/ci.yml/badge.svg)](https://github.com/chris-peterson/assurance/actions/workflows/ci.yml)

Package | Latest Release |
:-------- | :------------ |
`Assurance` | [![NuGet version](https://img.shields.io/nuget/dt/Assurance.svg)](https://www.nuget.org/packages/assurance)

## Getting Started

`dotnet add package Assurance`

OR

`PM> Install-Package Assurance`

NOTE: This package uses [Spiffy](https://github.com/chris-peterson/spiffy#overview) logging.

## Example

Imagine discovering some legacy code:

```c#
    int i;
    for (i = 0; i < 1000000; i++) ;
    return i;
```

Naturally, you consider replacing this code with:

```c#
    return 1000000;
```

Since the first code is "legacy", changing it is "scary" because it has been running
that way for a long time and might have side-effects that others couple to.

The `Assurance` library allows you to evaluate 2 implementations side-by-side
and switch to better implementations with confidence.

```c#
    var result = (await Runner.RunInParallel(
        "CountToOneMillion",
        () =>
        {
            int i;
            for (i = 0; i < 1000000; i++) ;
            return i;
        },
        () =>
        {
            return 1000000;
        }))
        .UseExisting();
        // .UseReplacement();
```

When the above code is run, log entries are created, e.g.

```plaintext
[2021-07-28 21:33:32.144Z] Level=Info Component=Assurance Operation=CountToOneMillion
TimeElapsed=24.6 Result=same TimeElapsed_Existing=24.1 TimeElapsed_Replacement=0.2
Use=existing
```

Both implementations can be synchronous (`Func<T>`, as above) or asynchronous (`Func<Task<T>>`); `RunInParallel` has overloads for each.

`Result=same` gives us confidence that we haven't regressed behavior.

`TimeElapsed_Replacement < TimeElapsed_Existing` gives us confidence that we haven't regressed performance.

The returned object makes it easy to toggle implementations (i.e. choose which is the authority).

Generally you'd defer to the existing implementation for some evaluation period, then [cutover to the replacement](#cutting-over).

### Different Results

Imagine we weren't so lucky with our drop-in replacement and observed that 1% of the time, the replacement implementation computed a different result.
In these cases, `Result=different` can be found in the logs along with a `Differences` field describing what changed, e.g.

```plaintext
[2021-07-28 21:33:32.242Z] Level=Info Component=Assurance
Operation=ComputeResult TimeElapsed=500 Result=different Differences="1000001 != 1000000"
TimeElapsed_Existing=500 TimeElapsed_Replacement=100
```

Comparing objects rather than scalars names each field that differs, nesting and collection
indexes included:

```plaintext
Differences="Name: Alice != Bob; Address.City: Springfield != Shelbyville; Tags[1]: b != c"
```

Where the two sides differ by type, the type names are reported as the values, e.g.
`Value: System.Int32 != System.String`.

Each difference is one `;`-separated entry, so a comparison over a large object graph produces a
correspondingly large field. [Controlling What Is Logged](#controlling-what-is-logged) covers
logging a summary instead.

Sometimes, you might be willing to accept different behavior if, for example, the new code is substantially faster.

Other times, you might find out that the existing system is _wrong_ and you prefer the results from the new implementation.

### Controlling What Is Logged

By default, a run logs `Result` and, when the two implementations disagree, every difference the
comparison found. That default stops serving you in two situations:

- **There are far too many line items to list.** A deep comparison reports every leaf that
  differs, so a wide object graph produces a field with hundreds of entries in it. Nobody reads
  that, and the one entry that matters is buried in it.
- **You are paying by log volume.** If ingest is metered, emitting every difference on every run
  is an expensive way to learn something you could have learned from one of them. Logging just
  the first difference and iterating (fix it, run again, see the next) costs a fraction of
  collecting the whole set on every run, and you were going to fix them one at a time anyway.

Supply an `ILogStrategy<T>` to decide what a run writes. The simplest route is to
subclass `DefaultLogStrategy<T>` and `DefaultLogRun<T>` and override `LogRunResult`:

```c#
    using Assurance.Logging;

    class FirstDifferenceLogStrategy : DefaultLogStrategy<Order>
    {
        public override ILogRun<Order> Begin(string taskName)
        {
            return new FirstDifferenceLogRun(ProvidedEventContext, taskName);
        }
    }

    class FirstDifferenceLogRun : DefaultLogRun<Order>
    {
        public FirstDifferenceLogRun(EventContext eventContext, string taskName)
            : base(eventContext, taskName)
        {
        }

        public override void LogRunResult(RunResult<Order> result)
        {
            if (result.ResultComparison.AreEqual)
            {
                Log("Result", "same");
                return;
            }
            var differences = result.ResultComparison.Differences;
            Log("Result", "different");
            Log("DifferenceCount", differences.Count);
            Log("FirstDifference", differences[0]);
        }
    }
```

Pass it to the runner in place of an `EventContext`:

```c#
    var result = (await Runner.RunInParallel(
        "PriceOrder",
        () => LegacyPricer.Price(order),
        () => NewPricer.Price(order),
        logStrategy: new FirstDifferenceLogStrategy()))
        .UseExisting();
```

```plaintext
[2021-07-28 21:33:32.242Z] Level=Info Component=Assurance Operation=PriceOrder
TimeElapsed=500 Result=different DifferenceCount=87
FirstDifference="Address.City: Springfield != Shelbyville"
TimeElapsed_Existing=500 TimeElapsed_Replacement=100 Use=existing
```

`ResultComparison.Differences` is the list of differences, one entry each, so a log can report a
count, take the first, or pick out the ones it cares about. Rendering the list itself gives every
entry on one line, which is what the default log writes.

`Begin` is called once per run, so a single strategy can be shared across many runs
and each still reports in full. `Log` and `AppendToValue` are available anywhere in
the run if you want to record fields beyond the comparison itself.

To route logs somewhere `Spiffy` cannot reach, implement `ILogStrategy<T>` and
`ILogRun<T>` directly. `EventContext` may be `null` in that case; the runner creates
its own context to time the two implementations against, and notes in it that the
timings landed there.

### Exception Behavior

An exception that occurs in the _**existing**_ implementation will be logged and re-thrown.

An exception that occurs in the _**replacement**_ implementation will be logged only (i.e. **not** re-thrown).

### Cutting Over

Once you are satisified with the replacement implementation, cutting over is a simple code change, from `UseExisting` to `UseReplacement`, e.g.

```c#
    var result = (await Runner.RunInParallel(
        "CountToOneMillion",
        () =>
        {
            int i;
            for (i = 0; i < 1000000; i++) ;
            return i;
        },
        () =>
        {
            return 1000000;
        }))
        // .UseExisting();
        .UseReplacement();
```

After an evaluation period, the old implementation (and the `Assurance` scaffolding) can be removed, e.g.

```c#

    var result = CountToOneMillion();

    int CountToOneMillion()
    {
        return 1000000;
    }
```
