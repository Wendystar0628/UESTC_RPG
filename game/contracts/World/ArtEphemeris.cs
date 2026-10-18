using System.Numerics;
using static Campus.Contracts.ScalarMath;

namespace Campus.Contracts;

public enum ClockPreset { Morning, Noon, Dusk, Night }

/// <summary>The authored solar and lunar curves in SYS-TIME; no wall clock or calendar.</summary>
public static class ArtEphemeris
{
    public const double NightDepth = 24d;
    public const double ArcExponent = 1.15;

    public readonly record struct Track(double Rise, double Set, double RiseAz, double SetAz, double Height, double MoonHeight);

    public static Track At(Season season) => season switch
    {
        Season.Spring => new(410 / 60d, 1160 / 60d, 98, 262, 55, 48),
        Season.Summer => new(370 / 60d, 1195 / 60d, 85, 272, 62, 38),
        Season.Autumn => new(455 / 60d, 1085 / 60d, 113, 248.5, 41, 48),
        Season.Winter => new(475 / 60d, 1075 / 60d, 117, 244, 34, 48),
        _ => throw new ArgumentOutOfRangeException(nameof(season)),
    };

    public static Track At(double season)
    {
        var position = Wrap(season, 4);
        var index = (int)position;
        var a = At((Season)index);
        var b = At((Season)((index + 1) % 4));
        var t = position - index;
        return new(Lerp(a.Rise, b.Rise, t), Lerp(a.Set, b.Set, t),
            Lerp(a.RiseAz, b.RiseAz, t), Lerp(a.SetAz, b.SetAz, t),
            Lerp(a.Height, b.Height, t), Lerp(a.MoonHeight, b.MoonHeight, t));
    }

    public static double Preset(ClockPreset preset, Season season)
    {
        var track = At(season);
        return preset switch
        {
            ClockPreset.Morning => track.Rise + 40 / 60d,
            ClockPreset.Noon => (track.Rise + track.Set) / 2,
            ClockPreset.Dusk => track.Set - 35 / 60d,
            ClockPreset.Night => 21,
            _ => throw new ArgumentOutOfRangeException(nameof(preset)),
        };
    }

    // Phase 0..1 is the daylight arc; 1..2 is the following night arc.
    public static double Phase(double clock, double season)
    {
        var track = At(season);
        if (clock >= track.Rise && clock < track.Set)
            return (clock - track.Rise) / (track.Set - track.Rise);
        return 1 + Wrap(clock - track.Set, 24) / (24 - track.Set + track.Rise);
    }

    public static double ClockAtPhase(double phase, double season)
    {
        var track = At(season);
        return phase < 1 ? track.Rise + phase * (track.Set - track.Rise)
            : track.Set + (phase - 1) * (24 - track.Set + track.Rise);
    }

    public static double DuskEnd(Season season)
    {
        var track = At(season);
        return track.Set + (24 - track.Set + track.Rise) * Math.Asin(Math.Pow(7d / NightDepth, 1 / ArcExponent)) / Math.PI;
    }

    public static (double Altitude, double Azimuth) SunAt(double clock, double season)
    {
        var track = At(season);
        var phase = Phase(clock, season);
        double altitude, azimuth;
        if (phase < 1)
        {
            var sine = Math.Sin(Math.PI * phase);
            altitude = track.Height * Math.Pow(Math.Max(0, sine), ArcExponent);
            azimuth = Lerp(track.RiseAz, track.SetAz, phase);
            // Tiny authored calibration: the autumn hero frame is exactly 5.5 / 241 degrees,
            // while sunrise, sunset and the noon cap retain their table values.
            var autumnWeight = Math.Max(0, 1 - Math.Abs(Wrap(season - 2 + 2, 4) - 2));
            var autumn = At(Season.Autumn);
            var heroPhase = (17.5 - autumn.Rise) / (autumn.Set - autumn.Rise);
            var heroSine = Math.Sin(Math.PI * heroPhase);
            var shape = sine * (1 - sine);
            altitude += autumnWeight * (5.5 - autumn.Height * Math.Pow(heroSine, ArcExponent)) * shape / (heroSine * (1 - heroSine));
            azimuth += autumnWeight * (241 - Lerp(autumn.RiseAz, autumn.SetAz, heroPhase)) * sine / heroSine;
        }
        else
        {
            altitude = -NightDepth * Math.Pow(Math.Max(0, Math.Sin(Math.PI * (phase - 1))), ArcExponent);
            azimuth = Wrap(Lerp(track.SetAz, track.RiseAz + 360, phase - 1), 360);
        }
        return (altitude, azimuth);
    }

    public static (Body Sun, Body Moon) Evaluate(double clock, double season)
    {
        var track = At(season);
        var phase = Phase(clock, season);
        var (altitude, azimuth) = SunAt(clock, season);
        var moonPhase = Wrap(phase - 1, 2);
        var moonAltitude = moonPhase < 1
            ? track.MoonHeight * Math.Pow(Math.Max(0, Math.Sin(Math.PI * moonPhase)), ArcExponent)
            : -NightDepth * Math.Pow(Math.Max(0, Math.Sin(Math.PI * (moonPhase - 1))), ArcExponent);
        var moonAzimuth = moonPhase < 1 ? Lerp(62, 300, moonPhase) : Wrap(Lerp(300, 422, moonPhase - 1), 360);
        return (BodyAt(altitude, azimuth, Smoothstep((altitude + 2) / 6)),
            BodyAt(moonAltitude, moonAzimuth, .2 * Smoothstep((moonAltitude + 2) / 6)));
    }

    private static Body BodyAt(double altitude, double azimuth, double intensity)
    {
        var h = altitude * Math.PI / 180;
        var a = azimuth * Math.PI / 180;
        return new(new Vector3((float)(Math.Cos(h) * Math.Sin(a)), (float)Math.Sin(h),
            (float)(-Math.Cos(h) * Math.Cos(a))), (float)altitude, (float)azimuth, (float)intensity);
    }

    public static double Wrap(double value, double period)
    {
        var remainder = value % period;
        return remainder < 0 ? remainder + period : remainder;
    }
}
