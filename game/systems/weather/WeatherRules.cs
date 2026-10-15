using Campus.Contracts;

namespace Campus.Systems.Weather;

/// <summary>Season legality and the SYS-TIME art-sun boundaries used by forward generation.</summary>
public static class WeatherRules
{
    public static bool IsNative(WeatherKind kind, Season season)
    {
        ValidateSeason(season);
        return kind switch
        {
            WeatherKind.Clear or WeatherKind.FewClouds or WeatherKind.Cloudy or WeatherKind.Overcast or WeatherKind.Fog => true,
            WeatherKind.LightRain or WeatherKind.Rain or WeatherKind.AfterRain => season != Season.Winter,
            WeatherKind.Thunderstorm => season is Season.Spring or Season.Summer,
            WeatherKind.Snow or WeatherKind.AfterSnow => season == Season.Winter,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    public static WeatherKind MapToSeason(WeatherKind kind, Season season)
    {
        if (IsNative(kind, season)) return kind;
        return kind switch
        {
            WeatherKind.LightRain or WeatherKind.Rain => WeatherKind.Snow,
            WeatherKind.AfterRain => WeatherKind.AfterSnow,
            WeatherKind.Snow => WeatherKind.Rain,
            WeatherKind.AfterSnow => WeatherKind.AfterRain,
            WeatherKind.Thunderstorm => season == Season.Winter ? WeatherKind.Snow : WeatherKind.Rain,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    public static float DefaultIntensity(WeatherKind kind) => kind switch
    {
        WeatherKind.LightRain => .25f,
        WeatherKind.Rain => .35f,
        WeatherKind.Fog or WeatherKind.Snow or WeatherKind.Thunderstorm => .65f,
        WeatherKind.Clear or WeatherKind.FewClouds or WeatherKind.Cloudy or WeatherKind.Overcast or
            WeatherKind.AfterRain or WeatherKind.AfterSnow => 1f,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public static double MinimumHours(WeatherKind kind) => kind switch
    {
        WeatherKind.LightRain or WeatherKind.Rain => .75,
        WeatherKind.Thunderstorm => .5,
        WeatherKind.Snow => 2,
        WeatherKind.Fog or WeatherKind.Overcast => 1.5,
        WeatherKind.Clear or WeatherKind.FewClouds or WeatherKind.Cloudy or WeatherKind.AfterRain or WeatherKind.AfterSnow => 0,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public static bool IsPrecipitation(WeatherKind kind) => kind switch
    {
        WeatherKind.LightRain or WeatherKind.Rain or WeatherKind.Thunderstorm or WeatherKind.Snow => true,
        WeatherKind.Clear or WeatherKind.FewClouds or WeatherKind.Cloudy or WeatherKind.Overcast or WeatherKind.Fog or
            WeatherKind.AfterRain or WeatherKind.AfterSnow => false,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public static (double DawnFrom, double DawnTo, double DuskFrom, double DuskTo) Bands(Season season)
    {
        var (rise, set, height) = Sun(season);
        double day = set - rise, night = 24 - day;
        double nightArc = Math.Asin(Math.Pow(7.0 / 24, 1 / 1.15)) / Math.PI;
        double morningArc = Math.Asin(Math.Pow(3 / height, 1 / 1.15)) / Math.PI;
        double eveningArc = Math.Asin(Math.Pow(8 / height, 1 / 1.15)) / Math.PI;
        return (rise - night * nightArc, rise + day * morningArc, set - day * eveningArc, set + night * nightArc);
    }

    public static bool IsProtected(double hours, double offset, Season season)
    {
        var b = Bands(season);
        double clock = Clock(hours + offset);
        return clock >= b.DawnFrom && clock < b.DawnTo || clock >= b.DuskFrom && clock < b.DuskTo;
    }

    public static double AfterProtected(double hours, double offset, Season season)
    {
        var b = Bands(season);
        double clock = Clock(hours + offset);
        if (clock >= b.DawnFrom && clock < b.DawnTo) return hours + b.DawnTo - clock + 1e-8;
        if (clock >= b.DuskFrom && clock < b.DuskTo) return hours + b.DuskTo - clock + 1e-8;
        return hours;
    }

    public static bool IsNight(double hours, double offset, Season season)
    {
        var b = Bands(season);
        double c = Clock(hours + offset);
        return c < b.DawnFrom || c >= b.DuskTo;
    }

    public static double FogEnd(double earliest, double offset, Season season)
    {
        var b = Bands(season);
        double c = Clock(earliest + offset);
        if (c >= b.DawnTo && c < b.DuskFrom) return earliest;
        return earliest + Clock(b.DawnTo - c) + 1e-8;
    }

    public static double QuietEnd(double hours, double offset, Season season)
    {
        double c = Clock(hours + offset), end = Bands(season).DuskTo;
        return hours + Clock(end - c);
    }

    public static double Clock(double hours) => (hours % 24 + 24) % 24;
    public static void ValidateIntensity(float value)
    {
        if (!float.IsFinite(value) || value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value));
    }
    public static void ValidateSeason(Season season) => _ = Sun(season);

    private static (double Rise, double Set, double Height) Sun(Season season) => season switch
    {
        Season.Spring => (410 / 60.0, 1160 / 60.0, 55),
        Season.Summer => (370 / 60.0, 1195 / 60.0, 62),
        Season.Autumn => (455 / 60.0, 1085 / 60.0, 41),
        Season.Winter => (475 / 60.0, 1075 / 60.0, 34),
        _ => throw new ArgumentOutOfRangeException(nameof(season))
    };
}
