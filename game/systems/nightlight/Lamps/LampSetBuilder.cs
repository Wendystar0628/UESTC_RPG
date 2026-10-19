using System.Numerics;
using Campus.Contracts;
using Campus.Systems.Nightlight.Schedule;

namespace Campus.Systems.Nightlight.Lamps;

/// <summary>灯表输入的统计：给日志和门禁看，哪些灯具标记没能登记、为什么。</summary>
public sealed record LampSetReport(int Accepted, int UnknownChannel, int NotLamp, int UnknownColor, int Duplicates);

/// <summary>
/// 把各内容包给出的灯具标记（LightEmitter：灯头位置、所属通道、色名、级别、光斑半径）整理成灯表。
/// 灯号按灯脚坐标排序，与标记的给出顺序无关；两个阈值都按灯脚坐标的稳定哈希生成，同一份输入结果逐位相同。
/// 只收灯具类通道的标记；挂窗户类通道的不进灯表（计入 NotLamp）。开放入口的门口灯是灯具，挂 lamp.first（门口灯组）。
/// </summary>
public static class LampSetBuilder
{
    public static IReadOnlyList<LampRecord> Build(
        IEnumerable<LightEmitter> emitters,
        ScheduleEvaluator schedule,
        Func<PaletteToken, int?> paletteColumn,
        Func<float, float, float> groundY,
        uint seed,
        out LampSetReport report)
    {
        ArgumentNullException.ThrowIfNull(emitters);
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(paletteColumn);
        ArgumentNullException.ThrowIfNull(groundY);
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
            if (!float.IsFinite(ground)) throw new InvalidDataException("A lamp base needs prepared finite terrain.");
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
        report = new(lamps.Count, unknownChannel, notLamp, unknownColor, duplicates);
        return lamps;
    }
}
