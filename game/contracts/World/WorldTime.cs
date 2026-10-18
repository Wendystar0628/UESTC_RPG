using System.Globalization;

namespace Campus.Contracts;

/// <summary>Shared animation cadence, loop period and world-clock presentation.</summary>
public static class WorldTime
{
    public const float AnimLoopSeconds = 480f;
    public const double InitialAnimTime = AnimLoopSeconds / 2d;
    public const int DefaultStepRate = 12;
    public const int ThirtyFpsStepRate = 15;

    public static bool IsStepRate(int rate) => rate is DefaultStepRate or ThirtyFpsStepRate;

    public static int StepRate(int frameCap, bool captureMode) =>
        !captureMode && frameCap == 30 ? ThirtyFpsStepRate : DefaultStepRate;

    /// <summary>HH:MM on a 24-hour clock; map recommendations round to the nearest minute.</summary>
    public static string FormatClock(double hours, bool roundToMinute = false)
    {
        var clock = hours % 24d;
        if (clock < 0) clock += 24d;
        var hour = (int)clock;
        var minute = (int)((clock - hour) * 60d);
        if (roundToMinute)
        {
            var total = (int)Math.Round(clock * 60d) % (24 * 60);
            hour = total / 60;
            minute = total % 60;
        }
        return string.Create(CultureInfo.InvariantCulture, $"{hour:00}:{minute:00}");
    }
}
