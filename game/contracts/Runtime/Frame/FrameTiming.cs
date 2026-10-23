namespace Campus.Contracts;

/// <summary>Main-thread frame observations, independent of simulation and animation time.</summary>
public interface IFrameTiming
{
    /// <summary>
    /// Raw milliseconds between the previous and current process entry, published by the kernel
    /// at FramePriority.FadeClock before other frame consumers. Long intervals are not clamped or smoothed.
    /// Null until two process entries have been observed, including after the clock node is readied again.
    /// </summary>
    double? PreviousFrameMilliseconds { get; }
}
