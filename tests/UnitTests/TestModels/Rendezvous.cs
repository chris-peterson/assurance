using System;
using System.Threading;

namespace Assurance.UnitTests.TestModels;

/// <summary>
/// Neither side can report "met" unless the other is already running, so a run that executes one
/// implementation after the other times out here rather than shaking out as a slow pass.
/// </summary>
internal static class Rendezvous
{
    // long enough that a loaded CI box still schedules both implementations, short enough that a
    // regression to sequential execution fails the run rather than hanging it
    static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public const string Met = "met";
    public const string Alone = "alone";

    public static string Meet(ManualResetEventSlim mine, ManualResetEventSlim theirs)
    {
        mine.Set();
        return theirs.Wait(Timeout) ? Met : Alone;
    }
}
