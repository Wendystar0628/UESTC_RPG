using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Cinematic;
using Campus.Systems.Nightlight.Schedule;

namespace Campus.Systems.Nightlight.Lamps;

/// <summary>灯表输入的统计：给日志和门禁看，哪些灯具标记没能登记、为什么。</summary>
public sealed record LampSetReport(int Accepted, int UnknownChannel, int NotLamp, int UnknownColor, int Duplicates, int CueLamps);

/// <summary>
/// 把各内容包给出的灯具标记（LightEmitter：灯头位置、所属通道、色名、级别、光斑半径）整理成灯表。
/// 灯号按灯脚坐标排序，与标记的给出顺序无关；两个阈值都按灯脚坐标的稳定哈希生成，同一份输入结果逐位相同。
/// </summary>
public static class LampSetBuilder
{
    public static IReadOnlyList<LampRecord> Build(
        IEnumerable<LightEmitter> emitters,
        ScheduleEvaluator schedule,
        Func<PaletteToken, int?> paletteColumn,
        Func<float, float, float> groundY,
        uint seed,
        IReadOnlyList<LampCueDef> cues,
        out LampSetReport report)
    {
        ArgumentNullException.ThrowIfNull(emitters);
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(paletteColumn);
        ArgumentNullException.ThrowIfNull(groundY);
        ArgumentNullException.ThrowIfNull(cues);
        int unknownChannel = 0, notLamp = 0, unknownColor = 0, duplicates = 0;
        var byKey = new SortedDictionary<string, LampRecord>(StringComparer.Ordinal);
        foreach (var e in emitters)
        {
            if (!schedule.TrySlotOf(e.Channel, out var slot)) { unknownChannel++; continue; }
            var group = schedule.GroupOf(slot);
            if (group is null) { notLamp++; continue; }
            var column = paletteColumn(e.Color) ?? (schedule.Spec(slot).Color is { } fallback ? paletteColumn(fallback) : null);
            if (column is null) { unknownColor++; continue; }
            var ground = groundY(e.Position.X, e.Position.Z);
            if (!float.IsFinite(ground)) ground = 0f;
            var basePoint = new Vector3(e.Position.X, ground, e.Position.Z);
            var key = LampTable.Key(basePoint);
            if (byKey.ContainsKey(key)) { duplicates++; continue; }
            var energy = Math.Clamp(e.Energy, 0f, 1f);
            var radius = Math.Max(e.Range, 0f);
            byKey.Add(key, new LampRecord(
                basePoint, Math.Max(e.Position.Y - ground, 0f), radius, column.Value, energy, slot,
                LampTable.Threshold(group, seed, key), LampTable.KeepHash(seed, key), 0,
                LampTable.GlowFor(radius, energy)));
        }
        var lamps = byKey.Values.ToList();
        var cueLamps = AssignCue(lamps, schedule, cues);
        report = new(lamps.Count, unknownChannel, notLamp, unknownColor, duplicates, cueLamps);
        return lamps;
    }

    /// <summary>
    /// 过场"初灯追镜头"：路线带宽以内、属于该组通道的灯改挂 lamp.cue，保留阈值 κ 换成沿路线的弧长比例，
    /// 于是前沿从 0 推到 1 时灯按路线顺序亮起；不覆盖时 lamp.cue 与原通道同值，画面与不登记时相同。
    /// </summary>
    public static int AssignCue(List<LampRecord> lamps, ScheduleEvaluator schedule, IReadOnlyList<LampCueDef> cues)
    {
        var assigned = 0;
        foreach (var cue in cues)
        {
            if (cue.Route.Count < 2 || !schedule.File.Cue.Groups.TryGetValue(cue.Group, out var source)) continue;
            var sourceSlot = schedule.SlotOf(source);
            var length = RouteLength(cue.Route);
            if (length <= 0f) continue;
            for (var i = 0; i < lamps.Count; i++)
            {
                var l = lamps[i];
                if (l.Slot != sourceSlot) continue;
                var (distance, arc) = Project(cue.Route, new Vector2(l.Base.X, l.Base.Z));
                if (distance > cue.Band) continue;
                lamps[i] = l with { Slot = schedule.CueSlot, Keep = Math.Clamp(arc / length, 0f, 0.999f) };
                assigned++;
            }
        }
        return assigned;
    }

    public static float RouteLength(IReadOnlyList<Vector2> route)
    {
        var total = 0f;
        for (var i = 1; i < route.Count; i++) total += Vector2.Distance(route[i - 1], route[i]);
        return total;
    }

    /// <summary>点到折线的最近距离，以及最近点沿折线的弧长。</summary>
    public static (float Distance, float Arc) Project(IReadOnlyList<Vector2> route, Vector2 p)
    {
        var best = float.MaxValue;
        var bestArc = 0f;
        var arc = 0f;
        for (var i = 1; i < route.Count; i++)
        {
            var a = route[i - 1];
            var b = route[i];
            var ab = b - a;
            var len2 = ab.LengthSquared();
            var t = len2 > 0f ? Math.Clamp(Vector2.Dot(p - a, ab) / len2, 0f, 1f) : 0f;
            var d = Vector2.Distance(p, a + ab * t);
            if (d < best) { best = d; bestArc = arc + MathF.Sqrt(len2) * t; }
            arc += MathF.Sqrt(len2);
        }
        return (best, bestArc);
    }
}
