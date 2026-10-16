using Godot;

namespace Campus.Kernel.Frame;

/// <summary>Kernel monotonic clock for measured main-thread budgets; unrelated to world or animation time.</summary>
public static class FrameClock
{
    public static double MonotonicMilliseconds => Time.GetTicksUsec() / 1000d;
    public static float FadeSeconds { get; internal set; }
}
