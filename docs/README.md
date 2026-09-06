# <img src="favicon.svg" alt="Assurance" width="64" height="64" style="vertical-align: middle"> Assurance

A library to boost confidence when making code changes.

`Assurance` evaluates two implementations side-by-side, letting you switch to better implementations with confidence.

## Status

[![build](https://github.com/chris-peterson/assurance/actions/workflows/ci.yml/badge.svg)](https://github.com/chris-peterson/assurance/actions/workflows/ci.yml)

| Package | Latest Release |
|:--------|:--------------|
| `Assurance` | [![NuGet version](https://img.shields.io/nuget/dt/Assurance.svg)](https://www.nuget.org/packages/assurance) |

## Getting Started

```bash
dotnet add package Assurance
```

> [!NOTE]
> This package uses [Spiffy](https://chris-peterson.github.io/spiffy) logging.

## Example

Imagine discovering some legacy code:

```csharp
int i;
for (i = 0; i < 1000000; i++) ;
return i;
```

You consider replacing this with:

```csharp
return 1000000;
```

Since the first code is "legacy", changing it is "scary" because it has been running
that way for a long time and might have side-effects that others couple to.

`Assurance` allows you to evaluate both implementations side-by-side:

```csharp
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

Log output:

```text
[2021-07-28 21:33:32.144Z] Level=Info Component=Assurance Operation=CountToOneMillion TimeElapsed=24.6 Result=same TimeElapsed_Existing=24.1 TimeElapsed_Replacement=0.2 Use=existing
```

`Result=same` gives confidence that behavior hasn't regressed. `TimeElapsed_Replacement < TimeElapsed_Existing` gives confidence that performance hasn't regressed.

Both implementations can be synchronous (`Func<T>`) or asynchronous (`Func<Task<T>>`); `RunInParallel` has overloads for each. Either way the two run at once on the thread pool, so whatever they share has to be safe to touch from two threads, and thread-affine state on the calling thread doesn't reach them.

## Different Results

When the replacement computes a different result, `Result=different` appears in the logs along with a `Differences` field describing what changed:

```text
[2021-07-28 21:33:32.242Z] Level=Info Component=Assurance Operation=ComputeResult TimeElapsed=500 Result=different Differences="1000001 != 1000000" TimeElapsed_Existing=500 TimeElapsed_Replacement=100
```

Comparing objects rather than scalars names each field that differs, nesting and collection indexes included:

```text
Differences="Name: Alice != Bob; Address.City: Springfield != Shelbyville; Tags[1]: b != c"
```

Each difference is one `;`-separated entry, so a comparison over a large object graph produces a correspondingly large field. [Controlling What Is Logged](#controlling-what-is-logged) covers logging a summary instead.

## Controlling What Is Logged

By default a run logs `Result` and, when the two implementations disagree, every difference the comparison found, up to `DeepComparisonStrategy<T>.DefaultMaxDifferences` (100). Pass `new DeepComparisonStrategy<T>(maxDifferences: 500)` to move that ceiling.

Two situations call for logging something other than the differences themselves. A wide object graph differs in many places at once, and the entry you act on is somewhere in the middle of the list. Or ingest is metered, and the first difference tells you as much as the whole set, since you fix them one at a time regardless.

Supply an `ILogStrategy<T>` to decide what a run writes. The simplest route is to subclass `DefaultLogStrategy<T>` and `DefaultLogRun<T>` and override `LogRunResult`:

```csharp
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
        if (differences.Count > 0)
        {
            Log("FirstDifference", differences[0]);
        }
    }
}
```

Pass it to the runner:

```csharp
var result = (await Runner.RunInParallel(
    "PriceOrder",
    () => LegacyPricer.Price(order),
    () => NewPricer.Price(order),
    logStrategy: new FirstDifferenceLogStrategy()))
    .UseExisting();
```

```text
[2021-07-28 21:33:32.242Z] Level=Info Component=Assurance Operation=PriceOrder TimeElapsed=500 Result=different DifferenceCount=87 FirstDifference="Address.City: Springfield != Shelbyville" TimeElapsed_Existing=500 TimeElapsed_Replacement=100 Use=existing
```

A run logs the values it compared. Whatever you hand to `Assurance` reaches the log, credentials and personal data included, and an `ILogStrategy<T>` is how you keep a value out of it.

`Begin` is called once per run, so a strategy that opens its own event per run is safe to share across concurrent runs.

To record into an event of your own, so an `Assurance` run correlates with what surrounds it, pass `new DefaultLogStrategy<T>(eventContext)`. That form writes every run it begins into the one event, so give it to a single run at a time.

To route logs somewhere `Spiffy` cannot reach, implement `ILogStrategy<T>` and `ILogRun<T>` directly. `EventContext` may be `null` in that case; the runner creates its own context to time the two implementations against, and notes in it that the timings landed there.

## Exception Behavior

An exception in the **existing** implementation is logged and re-thrown.

An exception in the **replacement** implementation is logged only (not re-thrown).

## Cutting Over

Once satisfied with the replacement, cutting over is a simple code change — from `UseExisting` to `UseReplacement`:

```csharp
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

After an evaluation period, remove the old implementation and the `Assurance` scaffolding:

```csharp
var result = CountToOneMillion();

int CountToOneMillion()
{
    return 1000000;
}
```
