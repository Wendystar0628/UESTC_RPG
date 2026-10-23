namespace Campus.Kernel.Quality.Settings;

/// <summary>DECISION-309 / ENG-PERF-ADAPT: focused refresh following belongs to VSync; Godot's zero means no software cap.</summary>
public static class FrameLimitPolicy
{
    public const int UnfocusedFramesPerSecond = 30;

    public static int Resolve(float requested, bool focused)
    {
        var limit = requested switch
        {
            0 => 60,
            1 => 120,
            2 => 0,
            _ => throw new ArgumentOutOfRangeException(nameof(requested)),
        };
        return focused ? limit : UnfocusedFramesPerSecond;
    }

    public static string Reason(float requested, bool focused) => !focused ? "unfocused" : requested switch
    {
        0 or 1 => "requested",
        2 => "vsync",
        _ => throw new ArgumentOutOfRangeException(nameof(requested)),
    };
}
