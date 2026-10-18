using System.Numerics;
using Campus.Contracts;
using Campus.Systems.Weather.Precip;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Tests.Weather;

[TestSuite]
public sealed class PrecipModelTests
{
    private static WeatherVec Vec(float precip = 0f, float kind = 0f, float storm = 0f, float wet = 0f, float puddle = 0f) =>
        new(0.8f, 0f, 0f, 0.85f, precip, kind, wet, puddle, 1f, 0f, storm, 0f, 0f, 0f);

    [TestCase]
    public void BufferIsDeterministicIdentityAndInsideTheUnitCube()
    {
        var a = PrecipModel.BuildBuffer();
        var b = PrecipModel.BuildBuffer();
        AssertThat(a.Length == PrecipTuning.Capacity * PrecipTuning.FloatsPerInstance).OverrideFailureMessage("Buffer size differs from the pool capacity.").IsTrue();
        AssertThat(a.AsSpan().SequenceEqual(b)).OverrideFailureMessage("Instance buffer is not reproducible.").IsTrue();
        for (var i = 0; i < PrecipTuning.Capacity; i++)
        {
            var o = i * PrecipTuning.FloatsPerInstance;
            AssertThat(a[o] == 1f && a[o + 5] == 1f && a[o + 10] == 1f && a[o + 3] == 0f && a[o + 7] == 0f && a[o + 11] == 0f).OverrideFailureMessage("Instance transforms must be identity.").IsTrue();
            for (var k = 12; k < 16; k++) AssertThat(a[o + k] is >= 0f and < 1f).OverrideFailureMessage("Custom data must lie in [0, 1).").IsTrue();
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
            AssertThat(MathF.Abs(mean.X - 0.5f) < 0.04f && MathF.Abs(mean.Y - 0.5f) < 0.04f && MathF.Abs(mean.Z - 0.5f) < 0.04f).OverrideFailureMessage($"Prefix {count} is not spread evenly: {mean}").IsTrue();
        }
    }

    [TestCase]
    public void CountsFollowTheWeatherSpecAndTheDensitySetting()
    {
        AssertThat(PrecipModel.VisibleCount(Vec(), 1f) == 0).OverrideFailureMessage("Clear weather must draw nothing.").IsTrue();
        AssertThat(PrecipModel.VisibleCount(Vec(0.005f), 1f) == 0).OverrideFailureMessage("A trace of precipitation must draw nothing.").IsTrue();
        AssertThat(PrecipModel.VisibleCount(Vec(0.25f), 1f) == 1500).OverrideFailureMessage("Light rain should draw 1500 streaks.").IsTrue();
        AssertThat(PrecipModel.VisibleCount(Vec(1f), 1f) == 6000).OverrideFailureMessage("Heavy rain should draw 6000 streaks.").IsTrue();
        AssertThat(PrecipModel.VisibleCount(Vec(1f, storm: 1f), 1f) == PrecipTuning.Capacity).OverrideFailureMessage("A thunderstorm fills the pool.").IsTrue();
        AssertThat(PrecipModel.VisibleCount(Vec(1f, kind: 1f), 1f) == 4000).OverrideFailureMessage("Heavy snow should draw 4000 flakes.").IsTrue();
        AssertThat(PrecipModel.VisibleCount(Vec(0.25f), 0.5f) == 750).OverrideFailureMessage("Smooth preset halves the count.").IsTrue();
        AssertThat(PrecipModel.VisibleCount(Vec(0.25f), 0.75f) == 1125).OverrideFailureMessage("Balanced preset keeps three quarters.").IsTrue();
    }

    [TestCase]
    public void TiltFollowsTheRainClass()
    {
        AssertThat(MathF.Abs(PrecipModel.TiltDeg(Vec(0.25f), 0f) - 5f) < 1e-3f).OverrideFailureMessage("Light rain tilts 5 degrees.").IsTrue();
        AssertThat(MathF.Abs(PrecipModel.TiltDeg(Vec(0.9f), 0f) - 15f) < 1e-3f).OverrideFailureMessage("Heavy rain tilts 15 degrees.").IsTrue();
        AssertThat(MathF.Abs(PrecipModel.TiltDeg(Vec(1f, storm: 1f), 0f) - 25f) < 1e-3f).OverrideFailureMessage("A thunderstorm tilts 25 degrees.").IsTrue();
        var v = PrecipModel.Velocity(Vec(0.25f), 0f);
        AssertThat(MathF.Abs(v.Y + PrecipTuning.RainFall) < 1e-4f && v.X > 0f).OverrideFailureMessage("Rain falls at 7 m/s and leans downwind.").IsTrue();
    }

    [TestCase]
    public void DisplacementOnlyMovesOnStepsAndWrapsWithoutJumps()
    {
        var model = new PrecipModel();
        var w = Vec(0.25f);
        var first = model.Step(w, 1, 479.75, 0f, 1f, 12);
        AssertThat(first.D.X == 0f && first.D.Y == 0f && first.D.Z == 0f).OverrideFailureMessage("The first frame only records the step.").IsTrue();
        var same = model.Step(w, 1, 479.75, 0f, 1f, 12);
        AssertThat(same.D == first.D).OverrideFailureMessage("Displacement moved without a step change.").IsTrue();
        var next = model.Step(w, 1, 479.75 + 1.0 / 12.0, 0f, 1f, 12);
        var step = PrecipModel.Velocity(w, 0f) / 12f;
        var period = PrecipTuning.WrapPerHalf * PrecipTuning.RainHalf;
        AssertThat(Vector3.Distance(Xyz(next.D), PrecipModel.Wrap(step, period)) < 1e-3f).OverrideFailureMessage("One step should move one step length.").IsTrue();
        AssertThat(MathF.Abs(next.V.W - step.Length()) < 1e-4f && next.D.W == PrecipTuning.RainHalf).OverrideFailureMessage("Streak length is one step; rain box is 18 m.").IsTrue();
        var wrapped = model.Step(w, 1, 0.0, 0f, 1f, 12);   // anim_time wrapped from 479.83 to 0
        AssertThat(Vector3.Distance(Xyz(wrapped.D), PrecipModel.Wrap(step * 3f, period)) < 1e-3f).OverrideFailureMessage("The 480 s wrap must count as two more steps.").IsTrue();
        var jumped = model.Step(w, 1, 200.0, 0f, 1f, 12);
        AssertThat(jumped.D == wrapped.D).OverrideFailureMessage("Setting the animation clock is not elapsed time.").IsTrue();
        var reloaded = model.Step(w, 2, 300.0, 0f, 1f, 12);
        AssertThat(reloaded.D == wrapped.D).OverrideFailureMessage("A new world instance restarts from the current step without moving.").IsTrue();
        for (var k = 0; k < 3; k++)
            AssertThat(reloaded.D[k] >= 0f && reloaded.D[k] < period).OverrideFailureMessage("Displacement is wrapped to 8h.").IsTrue();
    }

    private static Vector3 Xyz(Vector4 v) => new(v.X, v.Y, v.Z);

    [TestCase]
    public void SnowUsesItsOwnBoxAndFlakes()
    {
        var model = new PrecipModel();
        var frame = model.Step(Vec(0.6f, kind: 1f), 1, 10.0, 0.5f, 1f, 15);
        AssertThat(frame.D.W == PrecipTuning.SnowHalf && frame.K.X == 1f && frame.V.W == 0f).OverrideFailureMessage("Snow uses the 14 m box and flat flakes.").IsTrue();
        AssertThat(frame.VisibleCount == 2400).OverrideFailureMessage("Moderate snow draws 2400 flakes.").IsTrue();
    }

    [TestCase]
    public void GlobalsCarryTheWeatherVector()
    {
        var clear = new WeatherVec(0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0);
        AssertThat(WeatherGlobalsMap.A(clear) == Vector4.Zero && WeatherGlobalsMap.B(clear) == Vector4.Zero).OverrideFailureMessage("Clear weather writes zeros.").IsTrue();
        AssertThat(WeatherGlobalsMap.Fog(clear) == new Vector4(1, 0, 0, 0)).OverrideFailureMessage("Clear weather keeps the fog multiplier at 1.").IsTrue();
        var rain = Vec(0.4f, wet: 0.6f, puddle: 0.3f);
        AssertThat(WeatherGlobalsMap.A(rain) == new Vector4(0.8f, 0.85f, 0.4f, 0f)).OverrideFailureMessage("weather_a packs cloud, overcast, precipitation and kind.").IsTrue();
        AssertThat(WeatherGlobalsMap.B(rain).X == 0.6f && WeatherGlobalsMap.B(rain).Y == 0.3f).OverrideFailureMessage("weather_b packs wetness and puddles.").IsTrue();
        AssertThat(WeatherGlobalsMap.Fog(rain).W == 0.4f && WeatherGlobalsMap.Fog(Vec(0.4f, kind: 1f)).W == 0f).OverrideFailureMessage("The far rain curtain only exists in rain.").IsTrue();
    }

    [TestCase]
    public void PoolBoundsCoverTheReachableRange()
    {
        var box = PrecipModel.PoolBounds(new Vector2(50, 77), 3000, -28, 2000);
        AssertThat(box.Min.X <= 50 - 3000 && box.Max.Z >= 77 + 3000 && box.Min.Y <= -28 && box.Max.Y >= 2000).OverrideFailureMessage("Pool bounds must cover centre ± 3 km.").IsTrue();
    }

    [TestCase]
    public void PuddlesGrowFromTheSameCellsAsTheyDry()
    {
        AssertThat(!WetSurface.IsPuddle(3.2f, -7.7f, 0f)).OverrideFailureMessage("No puddles without standing water.").IsTrue();
        var full = 0;
        var half = 0;
        var nested = true;
        for (var k = 0; k < 6400; k++)
        {
            float x = -200f + (k / 80) * 0.73f, z = 40f + (k % 80) * 0.61f;
            var p = WetSurface.PuddlePotential(x, z);
            AssertThat(p is >= 0f and <= 1f).OverrideFailureMessage("Puddle potential must stay in [0, 1].").IsTrue();
            var a = WetSurface.IsPuddle(x, z, 1f);
            var b = WetSurface.IsPuddle(x, z, 0.5f);
            if (a) full++;
            if (b) half++;
            if (b && !a) nested = false;
        }
        var share = full / 6400f;
        AssertThat(share is > 0.12f and < 0.45f).OverrideFailureMessage($"Full puddles should cover roughly a third of hard ground, got {share:P0}.").IsTrue();
        AssertThat(half < full && nested).OverrideFailureMessage("Drying shrinks puddles in place; it never opens new ones.").IsTrue();
        AssertThat(WetSurface.Exposure(1f, false) == 1f && WetSurface.Exposure(-1f, false) == 0f && WetSurface.Exposure(1f, true) == 0f).OverrideFailureMessage("Exposure: open ground 1, soffits 0, interiors 0.").IsTrue();
        AssertThat(WetSurface.PuddleLevel(1f, 0f, 1f) == 0f && WetSurface.PuddleLevel(1f, 1f, 0f) == 0f).OverrideFailureMessage("Glass and walls never pool.").IsTrue();
    }
}
