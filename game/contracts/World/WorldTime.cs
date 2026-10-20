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

/// <summary>World-control transition timings and presentation rules (SYS-TIME, SYS-MOTION).</summary>
public static class WorldTimeTuning
{
    public const double SeasonSingleSeconds = 4;
    public const double SeasonMultipleSeconds = 5.5;
    public const double SeasonFogStart = .6;
    public const double SeasonFogPeak = 2;
    public const double SeasonFogRelease = 2.6;
    public const float SeasonGlowBoost = .1f;
    public const float SeasonBrightnessBoost = .25f;
    public const double ReducedTransitionSeconds = .5;
    public const double MaxFrameSeconds = .1;
    public const double DragIdleSeconds = 1;
    public const double ClockDamping = 2 * .6931471805599453 / .15;
    public const double CompletionEpsilon = 1e-10;
    public const float AngularSpeedSunAltitude = -7f;
    public const double JumpMaxSeconds = 3;
    public const double JumpBaseSeconds = .6;
    public const double JumpSecondsPerHour = .18;
    public const double WeatherPinBeforeHours = .5;
    public const double WeatherPinAfterHours = 2;
    public const double HeroBeforeHours = 8 / 60d;
    public const double HeroAfterHours = 10 / 60d;
}
