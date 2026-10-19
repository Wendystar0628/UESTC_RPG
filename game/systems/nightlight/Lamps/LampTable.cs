using System.Globalization;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Systems.Nightlight.Schedule;

namespace Campus.Systems.Nightlight.Lamps;

/// <summary>
/// lamp_table（N×2 RGBA32F）与 lamp_grid（map_rect 上 8 m 一格，RGBA32F，每格 4 个灯号）的打包。
/// 编码与 fx_glow、toon.gdshaderinc、precip 里的解码一致：
///   行 0 = (x, y_base, z, r)；行 1 = (色号低字节 + 256 × 能量百分比 + 32768 × bank, 槽位 + 32 × 灯头分米 + 32768 × 事件序号, θ, κ)；
///   lamp_grid 每个分量 = 灯号 + 1，0 为空，有灯的分量排在前面。
/// </summary>
public static class LampTable
{
    public const float CellMetres = 8f;
    public const int MaxLamps = 16384;          // 纹理宽度上限
    public const int SlotsPerCell = 4;
    public const float MaxPoolGain = 1.5f;      // 冬夜光斑面积 ×1.5（半径 ×√1.5）；格子按半径 ×1.5 的上限登记，留足余量，运行时不重建

    public readonly record struct GridSize(int Width, int Height);

    public static GridSize SizeFor(MapRect rect)
    {
        ArgumentNullException.ThrowIfNull(rect);
        var w = (int)Math.Ceiling((rect.Max.X - rect.Min.X) / CellMetres);
        var h = (int)Math.Ceiling((rect.Max.Y - rect.Min.Y) / CellMetres);
        if (w <= 0 || h <= 0) throw new ArgumentException("map_rect must have a positive extent.", nameof(rect));
        return new(w, h);
    }

    /// <summary>行 1 第一个分量：色号与能量百分比；bank 位在 bit 15（PaletteEncoding.PackLampColor）。色号 0–255 的结果与旧编码逐位相同。</summary>
    public static float PackColor(int column, float energy) =>
        PaletteEncoding.PackLampColor(new PaletteIndex(Math.Clamp(column, 0, PaletteLayout.Capacity - 1)), energy);

    /// <summary>行 1 第二个分量：槽位、灯头高度（分米，0–1023）与事件序号。</summary>
    public static float PackSlot(int slot, float headHeight, int sequence) =>
        Math.Clamp(slot, 0, 31) + 32f * MathF.Round(Math.Clamp(headHeight, 0f, 102.3f) * 10f) + 32768f * Math.Clamp(sequence, 0, 500);

    /// <summary>写出 N×2 的灯表：返回长度 N × 2 × 4 的数组，先是 N 个行 0 纹素，再是 N 个行 1 纹素。</summary>
    public static float[] PackTable(IReadOnlyList<LampRecord> lamps)
    {
        ArgumentNullException.ThrowIfNull(lamps);
        if (lamps.Count > MaxLamps) throw new InvalidDataException($"{lamps.Count} lamps exceed lamp_table width {MaxLamps}.");
        var n = Math.Max(lamps.Count, 1);
        var data = new float[n * 2 * 4];
        for (var i = 0; i < lamps.Count; i++)
        {
            var l = lamps[i];
            var r0 = i * 4;
            data[r0] = l.Base.X; data[r0 + 1] = l.Base.Y; data[r0 + 2] = l.Base.Z; data[r0 + 3] = l.Radius;
            var r1 = (n + i) * 4;
            data[r1] = PackColor(l.ColorColumn, l.Energy);
            data[r1 + 1] = PackSlot(l.Slot, l.HeadHeight, l.Sequence);
            data[r1 + 2] = l.ThresholdDeg;
            data[r1 + 3] = l.Keep;
        }
        return data;
    }

    /// <summary>
    /// 写出 lamp_grid：每格最多 4 盏灯，按"格中心离光斑边缘的距离"取最近的 4 盏（同距按灯号），
    /// 半径按冬夜放大后的值登记。返回长度 宽 × 高 × 4 的数组。
    /// </summary>
    public static float[] PackGrid(IReadOnlyList<LampRecord> lamps, MapRect rect, out GridSize size, out int overflowCells)
    {
        ArgumentNullException.ThrowIfNull(lamps);
        size = SizeFor(rect);
        var cells = new List<(float Score, int Lamp)>?[size.Width * size.Height];
        for (var i = 0; i < lamps.Count; i++)
        {
            var l = lamps[i];
            if (l.Radius <= 0f) continue;
            var reach = l.Radius * MaxPoolGain + CellMetres * 0.7072f;
            var x0 = (int)Math.Floor((l.Base.X - reach - rect.Min.X) / CellMetres);
            var x1 = (int)Math.Floor((l.Base.X + reach - rect.Min.X) / CellMetres);
            var z0 = (int)Math.Floor((l.Base.Z - reach - rect.Min.Y) / CellMetres);
            var z1 = (int)Math.Floor((l.Base.Z + reach - rect.Min.Y) / CellMetres);
            for (var cz = Math.Max(z0, 0); cz <= Math.Min(z1, size.Height - 1); cz++)
                for (var cx = Math.Max(x0, 0); cx <= Math.Min(x1, size.Width - 1); cx++)
                {
                    var center = new Vector2(rect.Min.X + (cx + 0.5f) * CellMetres, rect.Min.Y + (cz + 0.5f) * CellMetres);
                    var d = Vector2.Distance(center, new Vector2(l.Base.X, l.Base.Z));
                    if (d > reach) continue;
                    var score = d - l.Radius * MaxPoolGain;
                    (cells[cz * size.Width + cx] ??= []).Add((score, i));
                }
        }
        var data = new float[size.Width * size.Height * 4];
        overflowCells = 0;
        for (var c = 0; c < cells.Length; c++)
        {
            var list = cells[c];
            if (list is null) continue;
            if (list.Count > SlotsPerCell) overflowCells++;
            list.Sort((a, b) => a.Score != b.Score ? a.Score.CompareTo(b.Score) : a.Lamp.CompareTo(b.Lamp));
            for (var k = 0; k < Math.Min(SlotsPerCell, list.Count); k++) data[c * 4 + k] = list[k].Lamp + 1;
        }
        return data;
    }

    /// <summary>每盏灯的稳定键：灯脚的厘米坐标，与登记顺序无关。</summary>
    public static string Key(Vector3 position) => string.Create(CultureInfo.InvariantCulture,
        $"{MathF.Round(position.X * 100f):0}:{MathF.Round(position.Y * 100f):0}:{MathF.Round(position.Z * 100f):0}");

    /// <summary>按分组在 [ToDeg, FromDeg] 里取哈希阈值；分组两端都 ≥ 89° 时为常亮。</summary>
    public static float Threshold(ThresholdGroup group, uint seed, string key)
    {
        ArgumentNullException.ThrowIfNull(group);
        var u = ScheduleMath.Unit(StableHash.Hash32(seed, "nightlight.theta", key));
        return group.ToDeg + (group.FromDeg - group.ToDeg) * u;
    }

    public static float KeepHash(uint seed, string key) =>
        ScheduleMath.Unit(StableHash.Hash32(seed, "nightlight.keep", key)) * 0.999f;

    /// <summary>灯头光晕半径：按光斑半径与级别推出（主线版；灯具标记带灯种以后改为按灯种取）。</summary>
    public static float GlowFor(float radius, float energy) =>
        Math.Clamp(0.18f * radius, 0.22f, 1.4f) * (0.6f + 0.4f * Math.Clamp(energy, 0f, 1f));
}
