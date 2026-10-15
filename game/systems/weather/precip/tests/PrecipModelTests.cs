using System.Numerics;
using Campus.Contracts;
using Campus.Systems.Weather.Precip;
using GdUnit4;

namespace Campus.Tests.Weather;

[TestSuite]
public sealed class PrecipModelTests
{
    private static WeatherVec Vec(float precip = 0f, float kind = 0f, float storm = 0f, float wet = 0f, float puddle = 0f) =>
        new(0.8f, 0f, 0f, 0.85f, precip, kind, wet, puddle, 1f, 0f, storm, 0f, 0f, 0f);

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    [TestCase]
    public void BufferIsDeterministicIdentityAndInsideTheUnitCube()
    {
        var a = PrecipModel.BuildBuffer();
        var b = PrecipModel.BuildBuffer();
        Check(a.Length == PrecipTuning.Capacity * PrecipTuning.FloatsPerInstance, "Buffer size differs from the pool capacity.");
        Check(a.AsSpan().SequenceEqual(b), "Instance buffer is not reproducible.");
        for (var i = 0; i < PrecipTuning.Capacity; i++)
        {
            var o = i * PrecipTuning.FloatsPerInstance;
            Check(a[o] == 1f && a[o + 5] == 1f && a[o + 10] == 1f && a[o + 3] == 0f && a[o + 7] == 0f && a[o + 11] == 0f,
                "Instance transforms must be identity.");
            for (var k = 12; k < 16; k++) Check(a[o + k] is >= 0f and < 1f, "Custom data must lie in [0, 1).");
        }
    }

    [TestCase]
    public void EveryPrefixIsAnEvenThinning()
    {
        var data = PrecipModel.BuildBuffer();
        foreach (var count in new[] { 750, 1500, 4000 })
        {
            var mean = Vector3.Zero;
            for (var i = 0; i < count; i++)
            {
                var o = i * PrecipTuning.FloatsPerInstance;
                mean += new Vector3(data[o + 12], data[o + 13], data[o + 14]);
            }
            mean /= count;
            Check(MathF.Abs(mean.X - 0.5f) < 0.04f && MathF.Abs(mean.Y - 0.5f) < 0.04f && MathF.Abs(mean.Z - 0.5f) < 0.04f,
                $"Prefix {count} is not spread evenly: {mean}");
        }
    }

    [TestCase]
    public void CountsFollowTheWeatherSpecAndTheDensitySetting()
    {
        Check(PrecipModel.VisibleCount(Vec(), 1f) == 0, "Clear weather must draw nothing.");
        Check(PrecipModel.VisibleCount(Vec(0.005f), 1f) == 0, "A trace of precipitation must draw nothing.");
        Check(PrecipModel.VisibleCount(Vec(0.25f), 1f) == 1500, "Light rain should draw 1500 streaks.");
        Check(PrecipModel.VisibleCount(Vec(1f), 1f) == 6000, "Heavy rain should draw 6000 streaks.");
        Check(PrecipModel.VisibleCount(Vec(1f, storm: 1f), 1f) == PrecipTuning.Capacity, "A thunderstorm fills the pool.");
        Check(PrecipModel.VisibleCount(Vec(1f, kind: 1f), 1f) == 4000, "Heavy snow should draw 4000 flakes.");
        Check(PrecipModel.VisibleCount(Vec(0.25f), 0.5f) == 750, "Smooth preset halves the count.");
        Check(PrecipModel.VisibleCount(Vec(0.25f), 0.75f) == 1125, "Balanced preset keeps three quarters.");
    }

    [TestCase]
    public void TiltFollowsTheRainClass()
    {
        Check(MathF.Abs(PrecipModel.TiltDeg(Vec(0.25f), 0f) - 5f) < 1e-3f, "Light rain tilts 5 degrees.");
        Check(MathF.Abs(PrecipModel.TiltDeg(Vec(0.9f), 0f) - 15f) < 1e-3f, "Heavy rain tilts 15 degrees.");
        Check(MathF.Abs(PrecipModel.TiltDeg(Vec(1f, storm: 1f), 0f) - 25f) < 1e-3f, "A thunderstorm tilts 25 degrees.");
        var v = PrecipModel.Velocity(Vec(0.25f), 0f);
        Check(MathF.Abs(v.Y + PrecipTuning.RainFall) < 1e-4f && v.X > 0f, "Rain falls at 7 m/s and leans downwind.");
    }

    [TestCase]
    public void DisplacementOnlyMovesOnStepsAndWrapsWithoutJumps()
    {
        var model = new PrecipModel();
        var w = Vec(0.25f);
        var first = model.Step(w, 1, 479.75, 0f, 1f, 12);
        Check(first.D.X == 0f && first.D.Y == 0f && first.D.Z == 0f, "The first frame only records the step.");
        var same = model.Step(w, 1, 479.75, 0f, 1f, 12);
        Check(same.D == first.D, "Displacement moved without a step change.");
        var next = model.Step(w, 1, 479.75 + 1.0 / 12.0, 0f, 1f, 12);
        var step = PrecipModel.Velocity(w, 0f) / 12f;
        var period = PrecipTuning.WrapPerHalf * PrecipTuning.RainHalf;
        Check(Vector3.Distance(Xyz(next.D), PrecipModel.Wrap(step, period)) < 1e-3f, "One step should move one step length.");
        Check(MathF.Abs(next.V.W - step.Length()) < 1e-4f && next.D.W == PrecipTuning.RainHalf, "Streak length is one step; rain box is 18 m.");
        var wrapped = model.Step(w, 1, 0.0, 0f, 1f, 12);   // anim_time wrapped from 479.83 to 0
        Check(Vector3.Distance(Xyz(wrapped.D), PrecipModel.Wrap(step * 3f, period)) < 1e-3f, "The 480 s wrap must count as two more steps.");
        var jumped = model.Step(w, 1, 200.0, 0f, 1f, 12);
        Check(jumped.D == wrapped.D, "Setting the animation clock is not elapsed time.");
        var reloaded = model.Step(w, 2, 300.0, 0f, 1f, 12);
        Check(reloaded.D == wrapped.D, "A new world instance restarts from the current step without moving.");
        for (var k = 0; k < 3; k++)
            Check(reloaded.D[k] >= 0f && reloaded.D[k] < period, "Displacement is wrapped to 8h.");
    }

    private static Vector3 Xyz(Vector4 v) => new(v.X, v.Y, v.Z);

    [TestCase]
    public void SnowUsesItsOwnBoxAndFlakes()
    {
        var model = new PrecipModel();
        var frame = model.Step(Vec(0.6f, kind: 1f), 1, 10.0, 0.5f, 1f, 15);
        Check(frame.D.W == PrecipTuning.SnowHalf && frame.K.X == 1f && frame.V.W == 0f, "Snow uses the 14 m box and flat flakes.");
        Check(frame.VisibleCount == 2400, "Moderate snow draws 2400 flakes.");
    }

    [TestCase]
    public void GlobalsCarryTheWeatherVector()
    {
        var clear = new WeatherVec(0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0);
        Check(WeatherGlobalsMap.A(clear) == Vector4.Zero && WeatherGlobalsMap.B(clear) == Vector4.Zero, "Clear weather writes zeros.");
        Check(WeatherGlobalsMap.Fog(clear) == new Vector4(1, 0, 0, 0), "Clear weather keeps the fog multiplier at 1.");
        var rain = Vec(0.4f, wet: 0.6f, puddle: 0.3f);
        Check(WeatherGlobalsMap.A(rain) == new Vector4(0.8f, 0.85f, 0.4f, 0f), "weather_a packs cloud, overcast, precipitation and kind.");
        Check(WeatherGlobalsMap.B(rain).X == 0.6f && WeatherGlobalsMap.B(rain).Y == 0.3f, "weather_b packs wetness and puddles.");
        Check(WeatherGlobalsMap.Fog(rain).W == 0.4f && WeatherGlobalsMap.Fog(Vec(0.4f, kind: 1f)).W == 0f, "The far rain curtain only exists in rain.");
    }

    [TestCase]
    public void PoolBoundsCoverTheReachableRange()
    {
        var box = PrecipModel.PoolBounds(new Vector2(50, 77), 3000, -28, 2000);
        Check(box.Min.X <= 50 - 3000 && box.Max.Z >= 77 + 3000 && box.Min.Y <= -28 && box.Max.Y >= 2000, "Pool bounds must cover centre ± 3 km.");
    }

    [TestCase]
    public void PuddlesGrowFromTheSameCellsAsTheyDry()
    {
        Check(!WetSurface.IsPuddle(3.2f, -7.7f, 0f), "No puddles without standing water.");
        var full = 0;
        var half = 0;
        var nested = true;
        for (var k = 0; k < 6400; k++)
        {
            float x = -200f + (k / 80) * 0.73f, z = 40f + (k % 80) * 0.61f;
            var p = WetSurface.PuddlePotential(x, z);
            Check(p is >= 0f and <= 1f, "Puddle potential must stay in [0, 1].");
            var a = WetSurface.IsPuddle(x, z, 1f);
            var b = WetSurface.IsPuddle(x, z, 0.5f);
            if (a) full++;
            if (b) half++;
            if (b && !a) nested = false;
        }
        var share = full / 6400f;
        Check(share is > 0.12f and < 0.45f, $"Full puddles should cover roughly a third of hard ground, got {share:P0}.");
        Check(half < full && nested, "Drying shrinks puddles in place; it never opens new ones.");
        Check(WetSurface.Exposure(1f, false) == 1f && WetSurface.Exposure(-1f, false) == 0f && WetSurface.Exposure(1f, true) == 0f,
            "Exposure: open ground 1, soffits 0, interiors 0.");
        Check(WetSurface.PuddleLevel(1f, 0f, 1f) == 0f && WetSurface.PuddleLevel(1f, 1f, 0f) == 0f, "Glass and walls never pool.");
    }
}
