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

## Different Results

When the replacement computes a different result, `Result=different` appears in the logs along with `Existing` and `Replacement` fields:

```text
[2021-07-28 21:33:32.242Z] Level=Info Component=Assurance Operation=ComputeResult TimeElapsed=500 Result=different Existing=1000001 Replacement=1000000 TimeElapsed_Existing=500 TimeElapsed_Replacement=100
```

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
