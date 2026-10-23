namespace Campus.Kernel.Quality.Settings;

/// <summary>DECISION-309 / DECISION-321: Godot applies the cap; this pure policy selects it from player intent and the reported refresh rate.</summary>
public static class FrameLimitPolicy
{
    public const int DefaultFramesPerSecond = 60;
    public const int HighFramesPerSecond = 120;

    public static (int MaxFps, string Reason) Resolve(float requested, bool vSyncEnabled, float refreshRate) =>
        requested switch
        {
            0 => (DefaultFramesPerSecond, "requested"),
            1 => (HighFramesPerSecond, "requested"),
            // ENG-PERF-ADAPT: synchronized refresh following has no additional software cap.
            2 when vSyncEnabled => (0, "vsync"),
            // Engine.MaxFps accepts whole frames per second, including fractional display rates rounded to the nearest integer.
            2 when float.IsFinite(refreshRate) && refreshRate >= 1 && refreshRate < int.MaxValue =>
                ((int)MathF.Round(refreshRate), "refresh-rate"),
            2 => (DefaultFramesPerSecond, "refresh-fallback"),
            _ => throw new ArgumentOutOfRangeException(nameof(requested)),
        };
}
