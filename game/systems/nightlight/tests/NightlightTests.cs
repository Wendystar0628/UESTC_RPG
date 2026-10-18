using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Cinematic;
using Campus.Contracts.Maps;
using Campus.Kernel.Lighting;
using Campus.Kernel.World;
using Campus.Systems.Nightlight.Cue;
using Campus.Systems.Nightlight.Glow;
using Campus.Systems.Nightlight.Lamps;
using Campus.Systems.Nightlight.Schedule;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Tests;

[TestSuite]
public sealed class NightlightTests
{
    private static IEnumerable<string> ScheduleFiles()
    {
        var files = Directory.GetFiles(RepositoryFixture.FilePath("game", "data", "maps"), "lighting-schedule.json", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal).ToArray();
        if (files.Length == 0) throw new InvalidOperationException("No lighting-schedule.json found.");
        return files;
    }

    private static ScheduleEvaluator Load(string path) =>
        new(JsonSerializer.Deserialize<LightingScheduleFile>(File.ReadAllBytes(path), ContractJson.Options)
            ?? throw new InvalidDataException(path));

    private static ScheduleEvaluator Default() => Load(ScheduleFiles().First());

    private static ScheduleInput At(double clock, float sunAlt, float overcast = 0f, float precip = 0f, double dt = 0) =>
        new(clock, sunAlt, overcast, precip, 1f, dt);

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void EveryMapScheduleMatchesTheChannelCatalog()
    {
        foreach (var path in ScheduleFiles())
        {
            var schedule = Load(path);
            for (var slot = 0; slot < LightChannelCatalog.Count; slot++)
                AssertThat(schedule.Spec(slot).Id == LightChannelCatalog.Id(slot)).OverrideFailureMessage($"{path}: slot {slot} differs from LightChannelCatalog.").IsTrue();
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void FirstLightThresholdsCoverDusk()
    {
        var s = Default();
        // 初灯组：阈值在 8°–4° 间均匀分布，5.5° 时约六成亮着
        var first = s.GroupOf(s.SlotOf(new ChannelId("lamp.first")))!;
        var lit = 0;
        var n = TestSamples.PerProperty;
        for (var sample = 0; sample < n; sample++)
        {
            var i = sample * 4000 / n;
            if (LampTable.Threshold(first, 7u, LampTable.Key(new Vector3(i * 3.1f, 0f, i * 0.7f))) > 5.5f) lit++;
        }
        AssertThat(lit / (float)n).OverrideFailureMessage($"{"first-light share at 5.5°"}: expected {0.625f:R}, got {lit / (float)n:R}.").IsEqualApprox(0.625f, TestTolerance.Proportion);
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void OvercastAndRainLightLampsEarlier()
    {
        var s = Default();
        AssertThat(s.EffectiveSun(At(17.5, 5.5f))).OverrideFailureMessage($"{"clear h_eff"}: expected {5.5f:R}, got {s.EffectiveSun(At(17.5, 5.5f)):R}.").IsEqualApprox(5.5f, TestTolerance.AngleDegrees);
        AssertThat(s.EffectiveSun(At(17.5, 5.5f, 0.8f, 0.5f))).OverrideFailureMessage($"{"rain h_eff"}: expected {5.5f - 6f * 0.8f - 4f * 0.5f:R}, got {s.EffectiveSun(At(17.5, 5.5f, 0.8f, 0.5f)):R}.").IsEqualApprox(5.5f - 6f * 0.8f - 4f * 0.5f, TestTolerance.AngleDegrees);
        var general = s.SlotOf(new ChannelId("window.general"));
        AssertThat(s.Target(general, At(17.5, 5.5f, 0.8f, 0.5f)) > s.Target(general, At(17.5, 5.5f)) + 0.2f).OverrideFailureMessage("rainy dusk windows light earlier").IsTrue();
    }

    [TestCase]
    [TestCategory(TestTag.Determinism)]
    [TestCategory(TestReason.Contract)]
    public void ValuesDependOnStateNotOnHistory()
    {
        var a = Default();
        var b = Default();
        var outA = new Vector2[32];
        var outB = new Vector2[32];
        a.Evaluate(At(23.0, -30f, dt: 0.016), outA);
        a.Evaluate(At(23.4, -31f, dt: 0.016), outA);
        a.Reset();
        a.Evaluate(At(21.0, -14f, dt: 0.016), outA);   // 换实例：直接落到新值
        b.Evaluate(At(21.0, -14f, dt: 0.016), outB);
        for (var i = 0; i < 32; i++)
            AssertThat(BitConverter.SingleToInt32Bits(outA[i].X) == BitConverter.SingleToInt32Bits(outB[i].X)
                && BitConverter.SingleToInt32Bits(outA[i].Y) == BitConverter.SingleToInt32Bits(outB[i].Y))
                .OverrideFailureMessage($"slot {i} depends on history after a new instance.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void FastChangesWidenTheWindowFadeAndPausedFramesSettle()
    {
        var s = Default();
        var output = new Vector2[32];
        var general = s.SlotOf(new ChannelId("window.general"));
        s.Evaluate(At(17.3, 7.5f, dt: 0.016), output);
        s.Evaluate(At(17.7, 3.0f, dt: 0.016), output);   // 一帧里大跳
        AssertThat(output[general].Y).OverrideFailureMessage($"{"Δ after a jump"}: expected {0.15f:R}, got {output[general].Y:R}.").IsEqualApprox(0.15f, TestTolerance.FloatRelative);
        for (var i = 0; i < 120; i++) s.Evaluate(At(17.7, 3.0f, dt: 0.016), output);
        AssertThat(output[general].Y).OverrideFailureMessage($"{"Δ after two paused seconds"}: expected {0.005f:R}, got {output[general].Y:R}.").IsEqualApprox(0.005f, TestTolerance.FloatRelative);
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void LampChannelsKeepEveryLampAtFullRetention()
    {
        var s = Default();
        var output = new Vector2[32];
        s.Evaluate(At(21.0, -14f), output);
        var first = s.SlotOf(new ChannelId("lamp.first"));
        // κ < 1 的灯都满亮：smoothstep(κ, κ + Δ, L) = 1 要求 L ≥ 1 + Δ
        AssertThat(output[first].X >= 1f + output[first].Y - TestTolerance.FloatRelative).OverrideFailureMessage("full retention must cover κ close to 1").IsTrue();
        var landscape = s.SlotOf(new ChannelId("lamp.landscape"));
        s.Evaluate(At(3.0, -40f), output);
        AssertThat(output[landscape].X / (1f + output[landscape].Y)).OverrideFailureMessage($"{"landscape lamps halve after 1:00"}: expected {0.5f:R}, got {output[landscape].X / (1f + output[landscape].Y):R}.").IsEqualApprox(0.5f, TestTolerance.FloatRelative);
        AssertThat(Pool(s, 1.5f)).OverrideFailureMessage($"{"sys.poolGain carries the winter gain"}: expected {1.5f:R}, got {Pool(s, 1.5f):R}.").IsEqualApprox(1.5f, TestTolerance.FloatRelative);
    }

    private static float Pool(ScheduleEvaluator s, float gain)
    {
        var output = new Vector2[32];
        s.Evaluate(new ScheduleInput(21.0, -14f, 0f, 0f, gain, 0), output);
        return output[s.PoolGainSlot].X;
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void CueChangesOnlyTheCueSlotsAndReleasesExactly()
    {
        var s = Default();
        var plain = new Vector2[32];
        var cued = new Vector2[32];
        var released = new Vector2[32];
        s.Evaluate(At(17.4, 6.3f), plain);
        var def = new LampCueDef("avenue", "firstLight", [new(0, 0), new(0, 300)], 30f, [new(16, 0), new(28, 300)], 2.0);
        var cue = new LampCue(def, 5.5f);
        s.Cue = cue.At(22.0);
        s.Evaluate(At(17.4, 6.3f), cued);
        for (var i = 0; i < 32; i++)
            if (i != s.CueSlot && i != s.CueSysSlot) AssertThat(plain[i] == cued[i]).OverrideFailureMessage($"cue changed slot {i}.").IsTrue();
        AssertThat(cued[s.CueSlot].X / (1f + cued[s.CueSlot].Y)).OverrideFailureMessage($"{"cue front halfway at 22 s"}: expected {0.5f:R}, got {cued[s.CueSlot].X / (1f + cued[s.CueSlot].Y):R}.").IsEqualApprox(0.5f, TestTolerance.FloatRelative);
        AssertThat(cued[s.CueSysSlot].Y).OverrideFailureMessage($"{"cue target altitude"}: expected {5.5f:R}, got {cued[s.CueSysSlot].Y:R}.").IsEqualApprox(5.5f, TestTolerance.AngleDegrees);
        s.Cue = null;
        s.Evaluate(At(17.4, 6.3f), released);
        for (var i = 0; i < 32; i++) AssertThat(plain[i] == released[i]).OverrideFailureMessage($"slot {i} differs after release.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void LampTableRoundTripsThroughTheShaderDecoding()
    {
        var lamp = new LampRecord(new Vector3(12.5f, 1.25f, -40f), 6.1f, 5.2f, 51, 0.75f, 23, 6.37f, 0.4321f, 7, 0.9f);
        var data = LampTable.PackTable([lamp]);
        // 与 lamp.gdshaderinc 的 lamp_fetch 同一解码（bit 15 是色号的 bank，PaletteEncoding.UnpackLampColor）
        var (col, energy) = ShaderLampColor(data[4]);
        var slot = (int)(data[5] % 32f);
        var head = MathF.Floor(data[5] / 32f) % 1024f * 0.1f;
        var seq = MathF.Floor(data[5] / 32768f);
        AssertThat(col == 51 && slot == 23 && seq == 7f).OverrideFailureMessage("colour, slot and sequence survive packing").IsTrue();
        AssertThat(energy).OverrideFailureMessage($"{"energy"}: expected {0.75f:R}, got {energy:R}.").IsEqualApprox(0.75f, TestTolerance.FloatRelative);
        AssertThat(head).OverrideFailureMessage($"{"head height"}: expected {6.1f:R}, got {head:R}.").IsEqualApprox(6.1f, TestTolerance.Position);
        AssertThat(data[6]).OverrideFailureMessage($"{"theta"}: expected {6.37f:R}, got {data[6]:R}.").IsEqualApprox(6.37f, TestTolerance.AngleDegrees);
        foreach (var (index, e) in new[] { (51, 0.75f), (255, 1f), (256, 0f), (320, 0.42f), (511, 1f) })
        {
            var packed = PaletteEncoding.PackLampColor(new PaletteIndex(index), e);
            var (sc, se) = ShaderLampColor(packed);
            var (ci, ce) = PaletteEncoding.UnpackLampColor(packed);
            AssertThat(sc == index && ci.Value == index).OverrideFailureMessage($"lamp colour {index} survives the bank-aware decode").IsTrue();
            AssertThat(se).OverrideFailureMessage($"{$"lamp energy {e} decodes the same in the shader and the contract"}: expected {ce:R}, got {se:R}.").IsEqualApprox(ce, TestTolerance.FloatRelative);
        }
        AssertThat(data[7]).OverrideFailureMessage($"{"kappa"}: expected {0.4321f:R}, got {data[7]:R}.").IsEqualApprox(0.4321f, TestTolerance.FloatRelative);
        var rect = new MapRect(new Vector2(-64f, -64f), new Vector2(64f, 64f));
        var grid = LampTable.PackGrid([lamp], rect, out var size, out _);
        var cx = (int)((lamp.Base.X - rect.Min.X) / LampTable.CellMetres);
        var cz = (int)((lamp.Base.Z - rect.Min.Y) / LampTable.CellMetres);
        AssertThat(size.Width == 16 && size.Height == 16).OverrideFailureMessage("8 m cells over map_rect").IsTrue();
        AssertThat(grid[(cz * size.Width + cx) * 4] == 1f).OverrideFailureMessage("lamp registered in its own cell as id + 1").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void WindowThresholdsStayUniformSoLightsTrackTheChannel()
    {
        var below = 0;
        var n = TestSamples.PerProperty;
        for (var sample = 0; sample < n; sample++)
        {
            var i = sample * 20000 / n;
            var baseId = (i / 400) * 400;
            if (ScheduleMath.WindowThreshold(i, baseId, 20) < 0.5f) below++;
        }
        AssertThat(below / (float)n).OverrideFailureMessage($"{"half of the windows below θ = 0.5"}: expected {0.5f:R}, got {below / (float)n:R}.").IsEqualApprox(0.5f, TestTolerance.Proportion);
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void WindowGlowFollowsTheInventoryDeclarations()
    {
        var dorm = LightChannelCatalog.Id(1);
        var facade = new WindowFacadeDecl(1000, 12, dorm, 4, 3, 2, new Vector3(10f, 0f, 5f), Vector3.UnitX, Vector3.UnitY,
            3.6f, 1.8f, 1.6f, [0.9f, 4.1f])
        {
            Skip = [1],
        };
        var loose = new WindowDecl(70000, 70000, 6, LightChannelCatalog.Id(6), new Vector3(0f, 3f, 0f), Vector3.UnitZ, Vector3.UnitY, 1f, 2f);
        var unknown = new WindowDecl(5, 0, 4, new ChannelId("window.nowhere"), Vector3.Zero, Vector3.UnitX, Vector3.UnitY, 1f, 1f);
        var set = GlowSets.Windows([facade], [loose, unknown], out var report);
        AssertThat(set.Kind == GlowKind.Window && set.Count == 6 && report.UnknownChannel == 1 && report.BadFacade == 0).OverrideFailureMessage($"5 facade cells + 1 loose window, got {set.Count}").IsTrue();
        // 第一格：窗号 = 基址 + 行 × 总列数 + 列起点 + 列；custom.z = 槽位 + 32 × 总列数（与玻璃的 UV2.y 同一写法）
        AssertThat(set.Buffer[12] == 1004f && set.Buffer[13] == 1000f && set.Buffer[14] == 1 + 32f * 12 && set.Buffer[15] == 0f).OverrideFailureMessage("first facade cell custom data").IsTrue();
        // 留空的局部格 1 被跳过：第二个实例是列 2
        AssertThat(set.Buffer[GlowSet.Stride + 12] == 1006f).OverrideFailureMessage("skipped cell is not emitted").IsTrue();
        var last = (set.Count - 1) * GlowSet.Stride;
        AssertThat(set.Buffer[last + 12] == 70000f && set.Buffer[last + 15] == GlowSets.OutskirtsFlag).OverrideFailureMessage("outskirts window carries the skip-override flag").IsTrue();
        AssertThat(set.Buffer[3 * GlowSet.Stride + 7]).OverrideFailureMessage($"{"second-row centre height"}: expected {4.1f + 0.8f:R}, got {set.Buffer[3 * GlowSet.Stride + 7]:R}.").IsEqualApprox(4.1f + 0.8f, TestTolerance.Position);
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void EntranceLampsStayFullFromDuskToDawn()
    {
        var s = Default();
        var first = s.SlotOf(new ChannelId("lamp.first"));
        var warm = new PaletteToken("lamp.warm");
        // 门口灯组：照 WP-13 的 EntranceLampPart.Emitter 挂 lamp.first、二级 0.75、暖色、光斑 2.4 m；旧写法挂楼的室内通道（窗户类）
        var emitters = Enumerable.Range(0, 48).Select(i =>
            new LightEmitter(new ChannelId("lamp.first"), new Vector3(i * 9.7f, 2.45f, i * -5.3f), warm, 0.75f, 2.4f)).ToList();
        emitters.Add(new LightEmitter(new ChannelId("interior.public"), new Vector3(-50f, 2.45f, 0f), warm, 0.75f, 2.4f));
        var lamps = LampSetBuilder.Build(emitters, s, _ => 51, (_, _) => 0f, 1u, [], out var report);
        AssertThat(report.Accepted == 48 && report.NotLamp == 1).OverrideFailureMessage($"48 entrance lamps registered, the window-channel one dropped; got {report}").IsTrue();
        AssertThat(lamps.All(l => l.Slot == first && l.ThresholdDeg is >= 4f and <= 8f && l.Energy == 0.75f)).OverrideFailureMessage("entrance lamps ride the first-light group").IsTrue();
        // 秋季一整天每 5 分钟：太阳高于阈值时不亮；低于阈值后亮着，保留比例满（1:00–5:30 也不按哈希熄一半）
        var output = new Vector2[32];
        int dayOff = 0, nightOn = 0;
        for (var minute = 0; minute < 24 * 60; minute += 5)
        {
            var clock = minute / 60.0;
            var sun = ArtEphemeris.Evaluate(clock, (double)Season.Autumn).Sun.AltDeg;
            s.Evaluate(At(clock, sun), output);
            foreach (var l in lamps)
            {
                var (on, keep) = LampState(l, sun, output[l.Slot]);
                if (sun > l.ThresholdDeg + 0.1f) { AssertThat(on == 0f).OverrideFailureMessage($"entrance lamp lit by day at {clock:F2} h ({sun:F1}°)").IsTrue(); dayOff++; }
                else if (sun < l.ThresholdDeg - 0.1f) { AssertThat(on == 1f && keep == 1f).OverrideFailureMessage($"entrance lamp not full at {clock:F2} h ({sun:F1}°)").IsTrue(); nightOn++; }
            }
        }
        AssertThat(dayOff > 0 && nightOn > 0).OverrideFailureMessage("the day covers both lit and unlit samples").IsTrue();
        // 17:40（3.7°）门口灯已全部亮起
        var dusk = ArtEphemeris.Evaluate(17 + 40 / 60.0, (double)Season.Autumn).Sun.AltDeg;
        AssertThat(lamps.All(l => l.ThresholdDeg > dusk)).OverrideFailureMessage("every entrance lamp is lit by 17:40").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void WindowChannelsStayWindows()
    {
        var s = Default();
        var output = new Vector2[32];
        s.Evaluate(At(23.0, -30f), output);
        // 窗户类通道仍按亮窗比例求值（不乘 1 + Δ），楼内公共区 22:30 后 0.3
        var interior = s.SlotOf(new ChannelId("interior.public"));
        AssertThat(s.Spec(interior).Kind == ChannelClass.Window && s.GroupOf(interior) is null).OverrideFailureMessage("interior.public stays a window channel").IsTrue();
        for (var slot = 0; slot < 12; slot++)
            AssertThat(s.Spec(slot).Kind == ChannelClass.Window && output[slot].X <= s.Spec(slot).Cap + TestTolerance.FloatRelative).OverrideFailureMessage($"window slot {slot} keeps its cap").IsTrue();
        // 窗户类通道上的窗格声明照常进窗光晕
        var facade = new WindowFacadeDecl(2000, 4, new ChannelId("interior.public"), 0, 4, 1, Vector3.Zero, Vector3.UnitX, Vector3.UnitY,
            3f, 1.5f, 1.5f, [1f]);
        var set = GlowSets.Windows([facade], [], out var report);
        AssertThat(set.Count == 4 && report.UnknownChannel == 0 && set.Buffer[14] == interior + 32f * 4).OverrideFailureMessage("window-channel facades still glow as windows").IsTrue();
    }

    /// <summary>lamp.gdshaderinc 的 lamp_fetch 对行 1 x 的解码：bank = floor(x / 32768)，余下的是色号低字节 + 256 × 能量%。</summary>
    private static (int Col, float Energy) ShaderLampColor(float x)
    {
        var bank = MathF.Floor(x / 32768f);
        var packed = x - 32768f * bank;
        return ((int)(packed % 256f) + 256 * (int)bank, MathF.Floor(packed / 256f) / 100f);
    }

    /// <summary>与 lamp.gdshaderinc 的 lamp_level 同一写法：开灯（太阳高度低于 θ）与保留比例（smoothstep(κ, κ + Δ, L)）。</summary>
    private static (float On, float Keep) LampState(LampRecord l, float heff, Vector2 channel) =>
        (1f - ScalarMath.Smoothstep(l.ThresholdDeg - 0.08f, l.ThresholdDeg + 0.08f, heff),
         ScalarMath.Smoothstep(l.Keep, l.Keep + Math.Max(channel.Y, 1e-3f), channel.X));

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void CueLampsFollowTheRouteOrder()
    {
        var s = Default();
        var def = new LampCueDef("avenue", "firstLight", [new(0, 0), new(0, -200)], 30f, [new(16, 0), new(28, 200)], 2.0);
        var emitters = Enumerable.Range(0, 8).Select(i =>
            new LightEmitter(new ChannelId("lamp.first"), new Vector3(8f, 6f, -i * 27f), new PaletteToken("lamp.warm"), 1f, 5f)).ToList();
        emitters.Add(new LightEmitter(new ChannelId("lamp.first"), new Vector3(200f, 6f, 0f), new PaletteToken("lamp.warm"), 1f, 5f));
        var lamps = LampSetBuilder.Build(emitters, s, _ => 51, (_, _) => 0f, 1u, [def], out var report);
        AssertThat(report.CueLamps == 8).OverrideFailureMessage($"eight avenue lamps join the cue, got {report.CueLamps}").IsTrue();
        var cueLamps = lamps.Where(l => l.Slot == s.CueSlot).OrderBy(l => -l.Base.Z).ToArray();
        for (var i = 1; i < cueLamps.Length; i++) AssertThat(cueLamps[i].Keep > cueLamps[i - 1].Keep).OverrideFailureMessage("κ grows along the route").IsTrue();
        AssertThat(lamps.Single(l => l.Base.X > 100f).Slot == s.SlotOf(new ChannelId("lamp.first"))).OverrideFailureMessage("lamps off the route keep their channel").IsTrue();
    }
}
