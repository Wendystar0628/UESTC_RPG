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

/// <summary>窗光晕的输入统计：通道不在目录里、窗台高度缺行的声明不进光晕（给日志和门禁看）。</summary>
public sealed record WindowGlowReport(int Windows, int UnknownChannel, int BadFacade);

public static class GlowSets
{
    public const float WindowStandOff = 0.02f;   // 离立面 2 cm
    public const float WindowHaloRatio = 0.25f;  // 每边外扩 = 窗宽 × 0.25：面片边长 1.5 倍窗宽，光晕半径不超过窗宽的一半（ART-STYLE §7.5）
    public const int OutskirtsFlag = 1;
    public const int OutskirtsFirstId = 65536;   // 外围的窗号专用段（contract-12 第 1 节），着色器跳过窗覆盖

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

    /// <summary>
    /// 窗灯光晕：每扇窗一个面片，基 = (Right × 宽, Up × 高, 法线)；custom = (窗号, 楼基址, 槽位 + 32 × 楼窗网格总列数, 标志)。
    /// 输入是灯具清单里的立面窗格（WindowFacadeDecl）与不规则单窗（WindowDecl），顺序不影响画面。
    /// </summary>
    public static GlowSet Windows(IReadOnlyList<WindowFacadeDecl> facades, IReadOnlyList<WindowDecl> loose, out WindowGlowReport report)
    {
        ArgumentNullException.ThrowIfNull(facades);
        ArgumentNullException.ThrowIfNull(loose);
        int unknownChannel = 0, badFacade = 0;
        var total = loose.Count;
        foreach (var f in facades) total += Math.Max(f.Columns, 0) * Math.Max(f.Rows, 0);
        var buffer = new float[total * GlowSet.Stride];
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        var n = 0;
        void Add(int id, int baseId, int slot, int totalColumns, Vector3 center, Vector3 right, Vector3 up, Vector3 normal, float sx, float sy)
        {
            Write(buffer, n++, right * sx, up * sy, normal, center,
                new Vector4(id, baseId, slot + 32f * totalColumns, id >= OutskirtsFirstId ? OutskirtsFlag : 0));
            min = Vector3.Min(min, center - new Vector3(sx + sy));
            max = Vector3.Max(max, center + new Vector3(sx + sy));
        }
        foreach (var f in facades)
        {
            var slot = LightChannelCatalog.Slot(f.Channel);
            if (slot < 0) { unknownChannel++; continue; }
            if (f.SillY.Count < f.Rows || f.Width <= 0f || f.Height <= 0f) { badFacade++; continue; }
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
                    Add(id, f.BaseId, slot, f.TotalColumns, center, right, up, normal, sx, sy);
                }
        }
        foreach (var w in loose)
        {
            var slot = LightChannelCatalog.Slot(w.Channel);
            if (slot < 0) { unknownChannel++; continue; }
            if (w.Width <= 0f || w.Height <= 0f) { badFacade++; continue; }
            var right = Vector3.Normalize(w.Right);
            var up = Vector3.Normalize(w.Up);
            var normal = Vector3.Normalize(Vector3.Cross(right, up));
            var margin = w.Width * WindowHaloRatio;
            Add(w.Id, w.BaseId, slot, w.TotalColumns, w.Center + normal * WindowStandOff, right, up, normal,
                w.Width + 2f * margin, w.Height + 2f * margin);
        }
        if (n * GlowSet.Stride != buffer.Length) Array.Resize(ref buffer, n * GlowSet.Stride);
        report = new(n, unknownChannel, badFacade);
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
