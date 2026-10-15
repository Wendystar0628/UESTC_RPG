using System.Numerics;
using Campus.Contracts;
using Campus.Systems.Nightlight.Lamps;

namespace Campus.Systems.Nightlight.Glow;

/// <summary>fx_glow 的分支，与着色器里的 fx_kind 一致（材质 uniform，不是 #define）。只在末尾追加。</summary>
public enum GlowKind
{
    Window = 0,        // 窗灯光晕：贴立面的面片
    Lamp = 1,          // 路灯与景观灯的灯头光晕：朝镜头的面片
    LampCone = 2,      // 光锥（雾夜、雨夜可见；主线留空）
    ShaftOutdoor = 3,  // 室外软边光柱（主线留空）
    ShaftIndoor = 4,   // 室内棱柱光柱（主线留空）
    Dust = 5,          // 细尘（主线留空）
    Firefly = 6,       // 萤火虫（主线留空）
}

/// <summary>
/// 一组 fx_glow 实例：MultiMesh 的整块缓冲，每个实例 16 个 float = Transform3D 的 12 个（行主序）+ INSTANCE_CUSTOM 的 4 个。
/// 亮度全由着色器读通道算，CPU 不逐盏更新；没亮的实例在 vertex() 里塌成 0 面积。
/// </summary>
public sealed record GlowSet(GlowKind Kind, int Count, float[] Buffer, Box3 Bounds)
{
    public const int Stride = 16;
}

/// <summary>
/// 一个立面的窗格布局（与申请里的 WindowFacadeDecl 同字段；契约合入后由它直接换过来）。
/// Origin 是列 0、行 0 窗洞的左下角，Right、Up 为单位向量，Right × Up 指向室外；
/// 窗号 = BaseId + 行 × TotalColumns + ColumnStart + 列。
/// </summary>
public sealed record WindowFacade(
    int BaseId, int TotalColumns, int Slot, int ColumnStart, int Columns, int Rows,
    Vector3 Origin, Vector3 Right, Vector3 Up, float Pitch, float Width, float Height,
    IReadOnlyList<float> SillY)
{
    /// <summary>立面内留空的局部格号（行 × Columns + 列），例如门位。</summary>
    public IReadOnlyList<int> Skip { get; init; } = [];
    /// <summary>外围的楼：窗号在外围专用段，着色器跳过窗覆盖。</summary>
    public bool Outskirts { get; init; }
}

public static class GlowSets
{
    public const float WindowStandOff = 0.02f;   // 离立面 2 cm
    public const float WindowHaloRatio = 0.25f;  // 每边外扩 = 窗宽 × 0.25：面片边长 1.5 倍窗宽，光晕半径不超过窗宽的一半（ART-STYLE §7.5）
    public const int OutskirtsFlag = 1;

    /// <summary>灯头光晕：实例原点在灯头，基为单位阵；custom = (灯号, 光晕半径, 0, 0)。</summary>
    public static GlowSet Lamps(IReadOnlyList<LampRecord> lamps)
    {
        ArgumentNullException.ThrowIfNull(lamps);
        var buffer = new float[lamps.Count * GlowSet.Stride];
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        for (var i = 0; i < lamps.Count; i++)
        {
            var l = lamps[i];
            var head = l.Head;
            Write(buffer, i, Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ, head, new Vector4(i, l.GlowRadius, 0f, 0f));
            var pad = new Vector3(l.GlowRadius * 2.5f);
            min = Vector3.Min(min, head - pad);
            max = Vector3.Max(max, head + pad);
        }
        return new(GlowKind.Lamp, lamps.Count, buffer, lamps.Count > 0 ? new Box3(min, max) : default);
    }

    /// <summary>窗灯光晕：每扇窗一个面片，基 = (Right × 宽, Up × 高, 法线)；custom = (窗号, 楼基址, 槽位 + 32 × 楼窗网格总列数, 标志)。</summary>
    public static GlowSet Windows(IReadOnlyList<WindowFacade> facades)
    {
        ArgumentNullException.ThrowIfNull(facades);
        var total = 0;
        foreach (var f in facades) total += Math.Max(f.Columns, 0) * Math.Max(f.Rows, 0) - f.Skip.Count;
        var buffer = new float[Math.Max(total, 0) * GlowSet.Stride];
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        var n = 0;
        foreach (var f in facades)
        {
            if (f.SillY.Count < f.Rows) throw new ArgumentException($"Facade at base {f.BaseId} needs one sill height per row.", nameof(facades));
            var right = Vector3.Normalize(f.Right);
            var up = Vector3.Normalize(f.Up);
            var normal = Vector3.Normalize(Vector3.Cross(right, up));
            var margin = f.Width * WindowHaloRatio;
            var sx = f.Width + 2f * margin;
            var sy = f.Height + 2f * margin;
            var skip = f.Skip.Count > 0 ? new HashSet<int>(f.Skip) : null;
            for (var r = 0; r < f.Rows; r++)
                for (var c = 0; c < f.Columns; c++)
                {
                    if (skip is not null && skip.Contains(r * f.Columns + c)) continue;
                    var id = f.BaseId + r * f.TotalColumns + f.ColumnStart + c;
                    var center = f.Origin + right * (c * f.Pitch + f.Width * 0.5f) + up * (f.SillY[r] + f.Height * 0.5f) + normal * WindowStandOff;
                    Write(buffer, n++, right * sx, up * sy, normal, center,
                        new Vector4(id, f.BaseId, f.Slot + 32f * f.TotalColumns, f.Outskirts ? OutskirtsFlag : 0));
                    min = Vector3.Min(min, center - new Vector3(sx + sy));
                    max = Vector3.Max(max, center + new Vector3(sx + sy));
                }
        }
        if (n * GlowSet.Stride != buffer.Length) Array.Resize(ref buffer, n * GlowSet.Stride);
        return new(GlowKind.Window, n, buffer, n > 0 ? new Box3(min, max) : default);
    }

    private static void Write(float[] buffer, int index, Vector3 bx, Vector3 by, Vector3 bz, Vector3 origin, Vector4 custom)
    {
        var o = index * GlowSet.Stride;
        buffer[o + 0] = bx.X; buffer[o + 1] = by.X; buffer[o + 2] = bz.X; buffer[o + 3] = origin.X;
        buffer[o + 4] = bx.Y; buffer[o + 5] = by.Y; buffer[o + 6] = bz.Y; buffer[o + 7] = origin.Y;
        buffer[o + 8] = bx.Z; buffer[o + 9] = by.Z; buffer[o + 10] = bz.Z; buffer[o + 11] = origin.Z;
        buffer[o + 12] = custom.X; buffer[o + 13] = custom.Y; buffer[o + 14] = custom.Z; buffer[o + 15] = custom.W;
    }
}
