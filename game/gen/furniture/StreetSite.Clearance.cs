using Campus.Gen.Geometry;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.World;

namespace Campus.Gen.Furniture;

internal sealed partial class StreetSite
{
    /// <summary>
    /// Registers the planted tree rows along <paramref name="lines"/> (a furnished road), <paramref name="offsets"/> metres off its
    /// centre line on both sides. Rows are kept clear as whole lines: a gap the planting leaves is not known here.
    /// </summary>
    public void AddRows(IEnumerable<NetworkLine> lines, IReadOnlyList<float> offsets)
    {
        foreach (var line in lines.OrderBy(l => l.Id.Value, StringComparer.Ordinal))
            foreach (var offset in offsets)
            {
                if (line.Line.Points.Count < 2) continue;
                var pad = new Vector2(offset + 2f);
                var min = line.Line.Points.Aggregate(new Vector2(float.MaxValue), Vector2.Min) - pad;
                var max = line.Line.Points.Aggregate(new Vector2(float.MinValue), Vector2.Max) + pad;
                _rows.AddBox(min, max, (line, offset));
            }
    }

    /// <summary>True when a tree stands, or a registered tree row runs, within <paramref name="reach"/> of <paramref name="p"/> (planting to back a bench onto).</summary>
    public bool PlantingNear(Vector2 p, float reach)
    {
        foreach (var tree in _trees.In(p))
            if (Vector2.Distance(p, tree) <= reach) return true;
        foreach (var (line, offset) in _rows.In(p))
            if (MathF.Abs(MathF.Sqrt(LineDistanceSquared(p, line.Line.Points)) - offset) <= reach) return true;
        return false;
    }

    /// <summary>True when a disc keeps <see cref="FurnitureStyle.RowClear"/> off every registered tree row.</summary>
    public bool OffRows(Vector2 p, float radius)
    {
        foreach (var (line, offset) in _rows.In(p))
            if (MathF.Abs(MathF.Sqrt(LineDistanceSquared(p, line.Line.Points)) - offset) < radius + FurnitureStyle.RowClear) return false;
        return true;
    }

    /// <summary>True when a disc keeps <paramref name="clear"/> off every carriageway surface (beyond its half width).</summary>
    public bool OffCarriageways(Vector2 p, float radius, float clear)
    {
        foreach (var road in _roads.In(p))
        {
            if (!Carriageway(road)) continue;
            var r = HalfWidth(road) + clear + radius;
            if (LineDistanceSquared(p, road.Line.Points) < r * r) return false;
        }
        return true;
    }

    /// <summary>True when a carriageway surface lies within <paramref name="reach"/> ahead of <paramref name="p"/> along <paramref name="facing"/>.</summary>
    public bool CarriagewayAhead(Vector2 p, Vector2 facing, float reach)
    {
        for (var d = 0f; d <= reach + 1e-3f; d += 0.5f)
            if (!OffCarriageways(p + facing * d, 0f, 0f)) return true;
        return false;
    }

    /// <summary>
    /// True when a disc of <paramref name="radius"/> at <paramref name="p"/> is free: inside the campus, off every outline by
    /// <see cref="FurnitureStyle.OutlineClear"/>, off every road surface (the own road's included, so bends cannot swallow it) and
    /// bridge, clear of trees and of the things already placed.
    /// </summary>
    /// <remarks>
    /// <paramref name="beside"/> is a walk the thing stands at the edge of: it only has to stay off that walk's surface; with
    /// <paramref name="onBeside"/> it stands on that walk (a node bench at a wide walk's edge) and the walk does not block it at all.
    /// </remarks>
    public bool Free(Vector2 p, float radius, NetworkLine? beside = null) => Blocker(p, radius, beside) is null;

    /// <summary>What keeps a disc from being <see cref="Free(Vector2, float, NetworkLine?)"/>: the first of "outside the campus", "an outline", "a road", "a bridge", "a tree" or "another item"; null when nothing does.</summary>
    /// <remarks>
    /// <paramref name="outlineClear"/> replaces <see cref="FurnitureStyle.OutlineClear"/> for building outlines (a plaza bench backed onto
    /// a wall); with <paramref name="walksPass"/> pedestrian lines (walks, steps) do not block: a plaza carrying a walk line (Opus review
    /// 13c2b rework).
    /// </remarks>
    public string? Blocker(Vector2 p, float radius, NetworkLine? beside = null, bool onBeside = false, float? outlineClear = null, bool walksPass = false)
    {
        if (!PlanGeometry.Contains(World.Boundary, p)) return "the campus edge";
        foreach (var o in _outlines.In(p))
        {
            var keep = radius + (o.Kind == "building" && outlineClear is { } wall ? wall : FurnitureStyle.OutlineClear);
            if (p.X < o.Min.X - keep || p.X > o.Max.X + keep || p.Y < o.Min.Y - keep || p.Y > o.Max.Y + keep) continue;
            if (PlanGeometry.Contains(o.Rings, p) || Edge(o.Rings, p) < keep) return $"a {o.Kind} outline";
        }
        foreach (var road in _roads.In(p))
        {
            if (onBeside && ReferenceEquals(road, beside)) continue;
            if (walksPass && IsPedestrian(road)) continue;
            var r = HalfWidth(road) + (ReferenceEquals(road, beside) ? 0f : FurnitureStyle.RoadClear) + radius;
            if (LineDistanceSquared(p, road.Line.Points) < r * r) return "a road surface";
        }
        foreach (var bridge in _bridges.In(p))
        {
            var r = HalfWidth(bridge) + 2f + radius;
            if (LineDistanceSquared(p, bridge.Line.Points) < r * r) return "a bridge";
        }
        foreach (var tree in _trees.In(p))
            if (Vector2.Distance(p, tree) < radius + FurnitureStyle.TreeClear) return "a tree";
        return _occupied.AnyAround(p, o => Vector2.Distance(p, o.P) < radius + o.R + FurnitureStyle.ItemGap) ? "another item" : null;
    }

    /// <summary>True when a pedestrian line's centre line passes within <paramref name="clear"/> of any of <paramref name="points"/>.</summary>
    public bool NearWalkLine(IReadOnlyList<Vector2> points, float clear)
    {
        foreach (var p in points)
            foreach (var road in _roads.In(p))
                if (IsPedestrian(road) && LineDistanceSquared(p, road.Line.Points) < clear * clear) return true;
        return false;
    }

    /// <summary>Parking areas (their outlines), in feature-ID order: where parked cars stand.</summary>
    public IReadOnlyList<PolygonRings> ParkingLots => _parking;

    /// <summary>What crowds a disc inside an area the outlines do not cover (a parking lot): "a tree" or "another item", or null.</summary>
    public string? Clutter(Vector2 p, float radius)
    {
        foreach (var tree in _trees.In(p))
            if (Vector2.Distance(p, tree) < radius + FurnitureStyle.TreeClear) return "a tree";
        return _occupied.AnyAround(p, o => Vector2.Distance(p, o.P) < radius + o.R + FurnitureStyle.ItemGap) ? "another item" : null;
    }

    /// <summary>Pedestrian network lines (footways, paths, pedestrian streets, steps), in ID order: where walkers reach a kerb.</summary>
    public IEnumerable<NetworkLine> PedestrianLines() =>
        World.Network.Roads.Where(r => Pedestrian(RoadType(r.Props)) && r.Line.Points.Count >= 2).OrderBy(r => r.Id.Value, StringComparer.Ordinal);

    /// <summary>A long footprint (bench, bin) as a capsule from <paramref name="a"/> to <paramref name="b"/>: both ends and the middle are free.</summary>
    public bool Free(Vector2 a, Vector2 b, float radius, NetworkLine? beside = null) => Blocker(a, b, radius, beside) is null;

    /// <summary>What keeps a capsule from being <see cref="Free(Vector2, Vector2, float, NetworkLine?)"/> (see <see cref="Blocker(Vector2, float, NetworkLine?)"/>).</summary>
    public string? Blocker(Vector2 a, Vector2 b, float radius, NetworkLine? beside = null, bool onBeside = false, float? outlineClear = null, bool walksPass = false) =>
        Blocker(a, radius, beside, onBeside, outlineClear, walksPass) ?? Blocker((a + b) * 0.5f, radius, beside, onBeside, outlineClear, walksPass) ?? Blocker(b, radius, beside, onBeside, outlineClear, walksPass);

    public void Occupy(Vector2 a, Vector2 b, float radius)
    {
        Occupy(a, radius);
        Occupy((a + b) * 0.5f, radius);
        Occupy(b, radius);
    }

    public void Occupy(Vector2 p, float radius)
    {
        _occupied.AddPoint(p, (p, radius));
    }
}
