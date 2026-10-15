using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Cinematic;
using Campus.Contracts.Maps;
using Campus.Kernel.Lighting;
using Campus.Systems.Nightlight.Cue;
using Campus.Systems.Nightlight.Lamps;
using Campus.Systems.Nightlight.Schedule;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class NightlightTests
{
    private static IEnumerable<string> ScheduleFiles()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "game", "data", "maps"))) dir = dir.Parent;
        if (dir is null) throw new InvalidOperationException("Repository root not found from the test directory.");
        var files = Directory.GetFiles(Path.Combine(dir.FullName, "game", "data", "maps"), "lighting-schedule.json", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal).ToArray();
        if (files.Length == 0) throw new InvalidOperationException("No lighting-schedule.json found.");
        return files;
    }

    private static ScheduleEvaluator Load(string path) =>
        new(JsonSerializer.Deserialize<LightingScheduleFile>(File.ReadAllBytes(path), ContractJson.Options)
            ?? throw new InvalidDataException(path));

    private static ScheduleEvaluator Default() => Load(ScheduleFiles().First());

    private static ScheduleInput At(double clock, float sunAlt, float overcast = 0f, float precip = 0f, double dt = 0, long instance = 1) =>
        new(clock, sunAlt, overcast, precip, 1f, dt, instance);

    private static void Near(float expected, float actual, float tolerance, string what)
    {
        if (MathF.Abs(expected - actual) > tolerance) throw new InvalidOperationException($"{what}: expected {expected}, got {actual}.");
    }

    private static void True(bool condition, string what)
    {
        if (!condition) throw new InvalidOperationException(what);
    }

    [TestCase]
    public void EveryMapScheduleMatchesTheChannelCatalog()
    {
        foreach (var path in ScheduleFiles())
        {
            var schedule = Load(path);
            for (var slot = 0; slot < LightChannelCatalog.Count; slot++)
                True(schedule.Spec(slot).Id == LightChannelCatalog.Id(slot), $"{path}: slot {slot} differs from LightChannelCatalog.");
        }
    }

    [TestCase]
    public void DefaultDuskMatchesTheDocumentedCurves()
    {
        var s = Default();
        var general = s.SlotOf(new ChannelId("window.general"));
        // SYS-TIME §6：17:22–17:40（6.9° → 3.7°）约 20% → 70%，17:30（5.5°）约 40%
        Near(0.2f, s.Target(general, At(17.37, 6.9f)), 0.06f, "window.general at 17:22");
        Near(0.4f, s.Target(general, At(17.5, 5.5f)), 0.05f, "window.general at 17:30");
        Near(0.7f, s.Target(general, At(17.67, 3.7f)), 0.08f, "window.general at 17:40");
        // 初灯组：阈值在 8°–4° 间均匀分布，5.5° 时约六成亮着
        var first = s.GroupOf(s.SlotOf(new ChannelId("lamp.first")))!;
        var lit = 0;
        const int n = 4000;
        for (var i = 0; i < n; i++)
            if (LampTable.Threshold(first, 7u, LampTable.Key(new Vector3(i * 3.1f, 0f, i * 0.7f))) > 5.5f) lit++;
        Near(0.625f, lit / (float)n, 0.03f, "first-light share at 5.5°");
    }

    [TestCase]
    public void OvercastAndRainLightLampsEarlier()
    {
        var s = Default();
        Near(5.5f, s.EffectiveSun(At(17.5, 5.5f)), 1e-4f, "clear h_eff");
        Near(5.5f - 6f * 0.8f - 4f * 0.5f, s.EffectiveSun(At(17.5, 5.5f, 0.8f, 0.5f)), 1e-4f, "rain h_eff");
        var general = s.SlotOf(new ChannelId("window.general"));
        True(s.Target(general, At(17.5, 5.5f, 0.8f, 0.5f)) > s.Target(general, At(17.5, 5.5f)) + 0.2f, "rainy dusk windows light earlier");
        Near(0.3f, s.Target(general, At(12.0, 40f, 1f, 0.6f)), 0.02f, "rainy daytime window floor");
    }

    [TestCase]
    public void ValuesDependOnStateNotOnHistory()
    {
        var a = Default();
        var b = Default();
        var outA = new Vector2[32];
        var outB = new Vector2[32];
        a.Evaluate(At(23.0, -30f, dt: 0.016), outA);
        a.Evaluate(At(23.4, -31f, dt: 0.016), outA);
        a.Evaluate(At(21.0, -14f, dt: 0.016, instance: 2), outA);   // 换实例：直接落到新值
        b.Evaluate(At(21.0, -14f, dt: 0.016, instance: 7), outB);
        for (var i = 0; i < 32; i++) True(outA[i] == outB[i], $"slot {i} depends on history after a new instance.");
    }

    [TestCase]
    public void FastChangesWidenTheWindowFadeAndPausedFramesSettle()
    {
        var s = Default();
        var output = new Vector2[32];
        var general = s.SlotOf(new ChannelId("window.general"));
        s.Evaluate(At(17.3, 7.5f, dt: 0.016), output);
        s.Evaluate(At(17.7, 3.0f, dt: 0.016), output);   // 一帧里大跳
        Near(0.15f, output[general].Y, 1e-4f, "Δ after a jump");
        for (var i = 0; i < 120; i++) s.Evaluate(At(17.7, 3.0f, dt: 0.016), output);
        Near(0.005f, output[general].Y, 1e-4f, "Δ after two paused seconds");
    }

    [TestCase]
    public void LampChannelsKeepEveryLampAtFullRetention()
    {
        var s = Default();
        var output = new Vector2[32];
        s.Evaluate(At(21.0, -14f), output);
        var first = s.SlotOf(new ChannelId("lamp.first"));
        // κ < 1 的灯都满亮：smoothstep(κ, κ + Δ, L) = 1 要求 L ≥ 1 + Δ
        True(output[first].X >= 1f + output[first].Y - 1e-6f, "full retention must cover κ close to 1");
        var landscape = s.SlotOf(new ChannelId("lamp.landscape"));
        s.Evaluate(At(3.0, -40f), output);
        Near(0.5f, output[landscape].X / (1f + output[landscape].Y), 1e-4f, "landscape lamps halve after 1:00");
        Near(1.5f, Pool(s, 1.5f), 1e-6f, "sys.poolGain carries the winter gain");
    }

    private static float Pool(ScheduleEvaluator s, float gain)
    {
        var output = new Vector2[32];
        s.Evaluate(new ScheduleInput(21.0, -14f, 0f, 0f, gain, 0, 1), output);
        return output[s.PoolGainSlot].X;
    }

    [TestCase]
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
            if (i != s.CueSlot && i != s.CueSysSlot) True(plain[i] == cued[i], $"cue changed slot {i}.");
        Near(0.5f, cued[s.CueSlot].X / (1f + cued[s.CueSlot].Y), 1e-4f, "cue front halfway at 22 s");
        Near(5.5f, cued[s.CueSysSlot].Y, 1e-6f, "cue target altitude");
        s.Cue = null;
        s.Evaluate(At(17.4, 6.3f), released);
        for (var i = 0; i < 32; i++) True(plain[i] == released[i], $"slot {i} differs after release.");
    }

    [TestCase]
    public void LampTableRoundTripsThroughTheShaderDecoding()
    {
        var lamp = new LampRecord(new Vector3(12.5f, 1.25f, -40f), 6.1f, 5.2f, 51, 0.75f, 23, 6.37f, 0.4321f, 7, 0.9f);
        var data = LampTable.PackTable([lamp]);
        // 与 fx_glow 的 lamp_fetch 同一解码
        var col = (int)(data[4] % 256f);
        var energy = MathF.Floor(data[4] / 256f) / 100f;
        var slot = (int)(data[5] % 32f);
        var head = MathF.Floor(data[5] / 32f) % 1024f * 0.1f;
        var seq = MathF.Floor(data[5] / 32768f);
        True(col == 51 && slot == 23 && seq == 7f, "colour, slot and sequence survive packing");
        Near(0.75f, energy, 1e-6f, "energy");
        Near(6.1f, head, 1e-4f, "head height");
        Near(6.37f, data[6], 1e-6f, "theta");
        Near(0.4321f, data[7], 1e-6f, "kappa");
        var rect = new MapRect(new Vector2(-64f, -64f), new Vector2(64f, 64f));
        var grid = LampTable.PackGrid([lamp], rect, out var size, out _);
        var cx = (int)((lamp.Base.X - rect.Min.X) / LampTable.CellMetres);
        var cz = (int)((lamp.Base.Z - rect.Min.Y) / LampTable.CellMetres);
        True(size.Width == 16 && size.Height == 16, "8 m cells over map_rect");
        True(grid[(cz * size.Width + cx) * 4] == 1f, "lamp registered in its own cell as id + 1");
    }

    [TestCase]
    public void WindowThresholdsStayUniformSoLightsTrackTheChannel()
    {
        var below = 0;
        const int n = 20000;
        for (var i = 0; i < n; i++)
        {
            var baseId = (i / 400) * 400;
            if (ScheduleMath.WindowThreshold(i, baseId, 20) < 0.5f) below++;
        }
        Near(0.5f, below / (float)n, 0.05f, "half of the windows below θ = 0.5");
    }

    [TestCase]
    public void CueLampsFollowTheRouteOrder()
    {
        var s = Default();
        var def = new LampCueDef("avenue", "firstLight", [new(0, 0), new(0, -200)], 30f, [new(16, 0), new(28, 200)], 2.0);
        var emitters = Enumerable.Range(0, 8).Select(i =>
            new LightEmitter(new ChannelId("lamp.first"), new Vector3(8f, 6f, -i * 27f), new PaletteToken("lamp.warm"), 1f, 5f)).ToList();
        emitters.Add(new LightEmitter(new ChannelId("lamp.first"), new Vector3(200f, 6f, 0f), new PaletteToken("lamp.warm"), 1f, 5f));
        var lamps = LampSetBuilder.Build(emitters, s, _ => 51, (_, _) => 0f, 1u, [def], out var report);
        True(report.CueLamps == 8, $"eight avenue lamps join the cue, got {report.CueLamps}");
        var cueLamps = lamps.Where(l => l.Slot == s.CueSlot).OrderBy(l => -l.Base.Z).ToArray();
        for (var i = 1; i < cueLamps.Length; i++) True(cueLamps[i].Keep > cueLamps[i - 1].Keep, "κ grows along the route");
        True(lamps.Single(l => l.Base.X > 100f).Slot == s.SlotOf(new ChannelId("lamp.first")), "lamps off the route keep their channel");
    }
}
