using System.Numerics;
using Campus.Contracts;
using Campus.Systems.Nightlight.Glow;

namespace Campus.Systems.Nightlight.Windows;

internal readonly record struct WindowCell(int Id, int BaseId, int Slot, int TotalColumns,
    Vector3 Center, Vector3 Right, Vector3 Up, Vector3 Normal, float Width, float Height)
{
    public Box3 Bounds => new(Center - new Vector3(Width + Height), Center + new Vector3(Width + Height));
}

/// <summary>One window-center owner rule shared by the bounds directory and tile expansion.</summary>
internal static class WindowCells
{
    public static IEnumerable<TileKey> Candidates(IReadOnlyList<WindowFacadeDecl> facades, IReadOnlyList<WindowDecl> loose)
    {
        var tiles = new HashSet<TileKey>();
        foreach (var f in facades)
        {
            if (!Valid(f)) continue;
            var right = Vector3.Normalize(f.Right);
            var up = Vector3.Normalize(f.Up);
            var normal = Vector3.Normalize(Vector3.Cross(right, up));
            var min = new Vector3(float.MaxValue);
            var max = new Vector3(float.MinValue);
            for (var r = 0; r < f.Rows; r++)
            {
                var first = Center(f, right, up, normal, r, 0);
                var last = Center(f, right, up, normal, r, f.Columns - 1);
                min = Vector3.Min(min, Vector3.Min(first, last));
                max = Vector3.Max(max, Vector3.Max(first, last));
            }
            var from = TileGrid.At(min);
            var to = TileGrid.At(max);
            for (var x = from.X; x <= to.X; x++)
                for (var z = from.Z; z <= to.Z; z++) tiles.Add(new(x, z));
        }
        foreach (var w in loose)
            if (w.Width > 0f && w.Height > 0f && LightChannelCatalog.Slot(w.Channel) >= 0)
                tiles.Add(TileGrid.At(w.Center + Vector3.Normalize(Vector3.Cross(w.Right, w.Up)) * GlowSets.WindowStandOff));
        return tiles.OrderBy(t => t.X).ThenBy(t => t.Z);
    }

    /// <summary>Directory bounds use clipped center segments, without constructing any per-window records.</summary>
    public static Box3? Bounds(IReadOnlyList<WindowFacadeDecl> facades, IReadOnlyList<WindowDecl> loose, TileKey tile)
    {
        Box3? result = null;
        var origin = TileGrid.Origin(tile);
        void Add(Vector3 min, Vector3 max, float pad)
        {
            var p = new Vector3(pad);
            var box = new Box3(min - p, max + p);
            result = result is { } previous
                ? new(Vector3.Min(previous.Min, box.Min), Vector3.Max(previous.Max, box.Max)) : box;
        }
        foreach (var f in facades)
        {
            if (!Valid(f)) continue;
            var right = Vector3.Normalize(f.Right);
            var up = Vector3.Normalize(f.Up);
            var normal = Vector3.Normalize(Vector3.Cross(right, up));
            var margin = f.Width * GlowSets.WindowHaloRatio;
            for (var r = 0; r < f.Rows; r++)
            {
                var first = Center(f, right, up, normal, r, 0);
                var lo = 0f;
                var hi = (float)(f.Columns - 1);
                if (!Clip(first.X, right.X * f.Pitch, origin.X, origin.X + TileGrid.BaseEdge, ref lo, ref hi) ||
                    !Clip(first.Z, right.Z * f.Pitch, origin.Y, origin.Y + TileGrid.BaseEdge, ref lo, ref hi)) continue;
                var a = first + right * (lo * f.Pitch);
                var b = first + right * (hi * f.Pitch);
                Add(Vector3.Min(a, b), Vector3.Max(a, b), f.Width + f.Height + 4f * margin);
            }
        }
        foreach (var w in loose)
        {
            if (w.Width <= 0f || w.Height <= 0f || LightChannelCatalog.Slot(w.Channel) < 0) continue;
            var center = w.Center + Vector3.Normalize(Vector3.Cross(w.Right, w.Up)) * GlowSets.WindowStandOff;
            if (TileGrid.At(center) == tile) Add(center, center, w.Width + w.Height + 4f * w.Width * GlowSets.WindowHaloRatio);
        }
        return result;
    }

    public static void Visit(IReadOnlyList<WindowFacadeDecl> facades, IReadOnlyList<WindowDecl> loose,
        TileKey tile, Action<WindowCell> add, out WindowGlowReport report)
    {
        var count = 0;
        var unknown = 0;
        var bad = 0;
        var origin = TileGrid.Origin(tile);
        foreach (var f in facades)
        {
            var slot = LightChannelCatalog.Slot(f.Channel);
            if (slot < 0) { unknown++; continue; }
            if (!Valid(f)) { bad++; continue; }
            var right = Vector3.Normalize(f.Right);
            var up = Vector3.Normalize(f.Up);
            var normal = Vector3.Normalize(Vector3.Cross(right, up));
            var margin = f.Width * GlowSets.WindowHaloRatio;
            for (var r = 0; r < f.Rows; r++)
            {
                var first = Center(f, right, up, normal, r, 0);
                var lo = 0f;
                var hi = (float)(f.Columns - 1);
                if (!Clip(first.X, right.X * f.Pitch, origin.X, origin.X + TileGrid.BaseEdge, ref lo, ref hi) ||
                    !Clip(first.Z, right.Z * f.Pitch, origin.Y, origin.Y + TileGrid.BaseEdge, ref lo, ref hi)) continue;
                for (var c = Math.Max(0, (int)MathF.Floor(lo)); c <= Math.Min(f.Columns - 1, (int)MathF.Ceiling(hi)); c++)
                {
                    if (f.Skip.Contains(r * f.Columns + c)) continue;
                    var center = Center(f, right, up, normal, r, c);
                    if (TileGrid.At(center) != tile) continue;
                    add(new(f.BaseId + r * f.TotalColumns + f.ColumnStart + c, f.BaseId, slot, f.TotalColumns,
                        center, right, up, normal, f.Width + 2f * margin, f.Height + 2f * margin));
                    count++;
                }
            }
        }
        foreach (var w in loose)
        {
            var slot = LightChannelCatalog.Slot(w.Channel);
            if (slot < 0) { unknown++; continue; }
            if (w.Width <= 0f || w.Height <= 0f) { bad++; continue; }
            var right = Vector3.Normalize(w.Right);
            var up = Vector3.Normalize(w.Up);
            var normal = Vector3.Normalize(Vector3.Cross(right, up));
            var center = w.Center + normal * GlowSets.WindowStandOff;
            if (TileGrid.At(center) != tile) continue;
            var margin = w.Width * GlowSets.WindowHaloRatio;
            add(new(w.Id, w.BaseId, slot, w.TotalColumns, center, right, up, normal,
                w.Width + 2f * margin, w.Height + 2f * margin));
            count++;
        }
        report = new(count, unknown, bad);
    }

    private static bool Valid(WindowFacadeDecl f) => f.Columns > 0 && f.Rows > 0 && f.SillY.Count >= f.Rows &&
        f.Width > 0f && f.Height > 0f && LightChannelCatalog.Slot(f.Channel) >= 0;

    private static Vector3 Center(WindowFacadeDecl f, Vector3 right, Vector3 up, Vector3 normal, int r, int c) =>
        f.Origin + right * (c * f.Pitch + f.Width * 0.5f) + up * (f.SillY[r] + f.Height * 0.5f) + normal * GlowSets.WindowStandOff;

    private static bool Clip(float first, float step, float min, float max, ref float lo, ref float hi)
    {
        if (step == 0f) return first >= min && first <= max;
        var a = (min - first) / step;
        var b = (max - first) / step;
        lo = Math.Max(lo, Math.Min(a, b));
        hi = Math.Min(hi, Math.Max(a, b));
        return lo <= hi;
    }
}
