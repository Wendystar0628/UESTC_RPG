using Campus.Contracts;
using Campus.Kernel.Frame;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Tests;

[TestSuite]
public sealed class FrameTimingTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    [RequireGodotRuntime]
    public void ReadyAndFirstProcessLeaveTheIntervalUnavailable()
    {
        var now = 0d;
        var clock = new FrameClockNode(() => now);
        IFrameTiming timing = clock;
        var originalFadeSeconds = FrameClock.FadeSeconds;
        try
        {
            clock._Ready();
            AssertThat(timing.PreviousFrameMilliseconds.HasValue).IsFalse();
            now = 3000;
            clock._Process(1 / 60d);
            AssertThat(timing.PreviousFrameMilliseconds.HasValue).IsFalse();
            now += 5;
            clock._Process(1 / 60d);
            AssertThat(timing.PreviousFrameMilliseconds == 5).IsTrue();

            clock._Ready();
            AssertThat(timing.PreviousFrameMilliseconds.HasValue).IsFalse();
            now += 2000;
            clock._Process(1 / 60d);
            AssertThat(timing.PreviousFrameMilliseconds.HasValue).IsFalse();
            now += 11;
            clock._Process(1 / 60d);
            AssertThat(timing.PreviousFrameMilliseconds == 11).IsTrue();
        }
        finally
        {
            clock.Free();
            FrameClock.FadeSeconds = originalFadeSeconds;
        }
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    [RequireGodotRuntime]
    public void RawIntervalPreservesAStallThatTheEngineDeltaDoesNotContain()
    {
        const double engineDeltaSeconds = 1 / 60d;
        const double stallMilliseconds = 200;
        var now = 1000d;
        var clock = new FrameClockNode(() => now);
        IFrameTiming timing = clock;
        var originalFadeSeconds = FrameClock.FadeSeconds;
        try
        {
            clock._Ready();
            clock._Process(engineDeltaSeconds);
            now += stallMilliseconds;
            AssertThat(stallMilliseconds > engineDeltaSeconds * 1000)
                .OverrideFailureMessage("The fixture must contain a stall absent from the supplied engine delta.").IsTrue();
            clock._Process(engineDeltaSeconds);
            AssertThat(timing.PreviousFrameMilliseconds == stallMilliseconds)
                .OverrideFailureMessage("Frame observations must retain the full monotonic interval.").IsTrue();

            const double followingFrameMilliseconds = 8;
            now += followingFrameMilliseconds;
            clock._Process(engineDeltaSeconds);
            AssertThat(timing.PreviousFrameMilliseconds == followingFrameMilliseconds).IsTrue();
        }
        finally
        {
            clock.Free();
            FrameClock.FadeSeconds = originalFadeSeconds;
        }
    }
}
