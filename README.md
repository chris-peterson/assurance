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

`Result=same` gives us confidence that we haven't regressed behavior.

`TimeElapsed_Replacement < TimeElapsed_Existing` gives us confidence that we haven't regressed performance.

The returned object makes it easy to toggle implementations (i.e. choose which is the authority).

Generally you'd defer to the existing implementation for some evaluation period, then [cutover to the replacement](#cutting-over).

### Different Results

Imagine we weren't so lucky with our drop-in replacement and observed that 1% of the time, the replacement implementation computed a different result.
In these cases, `Result=different` can be found in the logs along with `Existing` and `Replacement` fields, e.g.

```plaintext
[2021-07-28 21:33:32.242Z] Level=Info Component=Assurance
Operation=ComputeResult TimeElapsed=500 Result=different Existing=1000001 Replacement=1000000
TimeElapsed_Existing=500 TimeElapsed_Replacement=100
```

Sometimes, you might be willing to accept different behavior if, for example, the new code is substantially faster.

Other times, you might find out that the existing system is _wrong_ and you prefer the results from the new implementation.

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
