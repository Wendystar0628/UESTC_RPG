using System.Numerics;
using Campus.Contracts;
using Campus.Systems.Nightlight.Windows;

namespace Campus.Systems.Nightlight.Glow;

/// <summary>窗光晕的输入统计：通道不在目录里、窗台高度缺行的声明不进光晕（给日志和门禁看）。</summary>
public sealed record WindowGlowReport(int Windows, int UnknownChannel, int BadFacade);

public static class GlowSets
{
    public const float WindowStandOff = 0.02f;   // 离立面 2 cm
    public const float WindowHaloRatio = 0.25f;  // 每边外扩 = 窗宽 × 0.25：面片边长 1.5 倍窗宽，光晕半径不超过窗宽的一半（ART-STYLE）
    public const int OutskirtsFlag = 1;
    public const int OutskirtsFirstId = 65536;   // 外围的窗号专用段（contract-12 第 1 节），着色器跳过窗覆盖

    /// <summary>灯头光晕：实例原点在灯头，基为单位阵；custom = (灯号, 光晕半径, 0, 0)。</summary>
    public static GlowSet Lamps(IReadOnlyList<LampRecord> lamps, IReadOnlyList<int> ids)
    {
        var buffer = new float[ids.Count * GlowSet.Stride];
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        for (var i = 0; i < ids.Count; i++)
        {
            var id = ids[i];
            var l = lamps[id];
            Write(buffer, i, Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ, l.Head, new Vector4(id, l.GlowRadius, 0f, 0f));
            var pad = new Vector3(l.GlowRadius * 2.5f);
            min = Vector3.Min(min, l.Head - pad);
            max = Vector3.Max(max, l.Head + pad);
        }
        return new(GlowKind.Lamp, ids.Count, buffer, ids.Count > 0 ? new Box3(min, max) : default);
    }

    /// <summary>Expands only windows whose final center belongs to this owner tile; IDs and channel ABI stay global.</summary>
    public static GlowSet Windows(IReadOnlyList<WindowFacadeDecl> facades, IReadOnlyList<WindowDecl> loose,
        TileKey tile, out WindowGlowReport report)
    {
        var cells = new List<WindowCell>();
        WindowCells.Visit(facades, loose, tile, cells.Add, out report);
        var buffer = new float[cells.Count * GlowSet.Stride];
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        for (var i = 0; i < cells.Count; i++)
        {
            var w = cells[i];
            Write(buffer, i, w.Right * w.Width, w.Up * w.Height, w.Normal, w.Center,
                new Vector4(w.Id, w.BaseId, w.Slot + 32f * w.TotalColumns, w.Id >= OutskirtsFirstId ? OutskirtsFlag : 0));
            min = Vector3.Min(min, w.Bounds.Min);
            max = Vector3.Max(max, w.Bounds.Max);
        }
        return new(GlowKind.Window, cells.Count, buffer, cells.Count > 0 ? new Box3(min, max) : default);
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
