# Overview

`Assurance` is a library that helps boost confidence when making code changes.

![image](https://user-images.githubusercontent.com/1980791/111552805-504fdd00-8740-11eb-8086-444e52abef65.png)

## Getting Started

`PM> Install-Package Assurance`

NOTE: This package works with [Spiffy](https://github.com/chris-peterson/spiffy#overview) logging.

## Example

Imagine discovering some legacy code:

```c#
    int i;
    for (i = 0; i < 1000000; i++) { }
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
    await Runner.RunInParallel(
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
        });
```

When the above code is run, log entries are created, e.g.

```plaintext
[2021-07-28 21:33:32.144Z] Level=Info Component=Assurance Operation=CountToOneMillion TimeElapsed=24.6 Result=same TimeElapsed_Existing=24.1 TimeElapsed_Replacement=0.2
```

`Result=same` gives us confidence that we haven't regressed behavior.

`TimeElapsed_Replacement < TimeElapsed_Existing` gives us confidence that we haven't regressed performance.

The returned object makes it easy to `.UseExisting()` or `.UseReplacement()` to pivot between implementations.

Generally you'd defer to the existing implementation for some evaluation period, then switch over to replacement;
eventually the scaffolding can be removed leaving just the new, improved implementation.

### Different Results

Imagine we weren't so lucky with our drop-in replacement and observed that 1% of the time, the replacement implementation computed a different result.
In these cases, `Result=different` can be found in the logs along with `Existing` and `Replacement` fields, e.g.

```plaintext
[2021-07-28 21:33:32.242Z] Level=Info Component=Assurance Operation=ComputeResult TimeElapsed=500 Result=different Existing=1000000 Replacement=1000001 TimeElapsed_Existing=500 TimeElapsed_Replacement=100
```

Sometimes, you might be willing to accept different behavior if, for example, the new code is substantially faster.

Other times, you might find out that the existing system is _wrong_ and you prefer the new implementation.

### Exception Behavior

An exception that occurs in the _**existing**_ implementation will be logged and re-thrown.

An exception that occurs in the _**replacement**_ implementation will be logged only.
