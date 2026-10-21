using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Parts.Circulation.Geometry;
using S = Campus.Gen.Parts.Circulation.CirculationStyle;

namespace Campus.Gen.Parts.Circulation;

/// <summary>
/// Edge curbs and handrails of a ramp whose deck is the height field (WLD-TERRAIN "坡道是高度场的一部分，扶手按部件
/// 规则"), along any plan curve and grade: the time-plaza rim ramps and curved corridor ramps (MAP-QSH-TERRAIN).
/// <paramref name="deckLine"/> runs along the deck centreline at deck height; curbs and rails keep plumb sections on the
/// slope. The straight equivalent is <see cref="RampPart"/> variation 2. No collision: the height field carries walkers,
/// curbs are 0.1 m and handrails do not block (WLD-INTERIORS).
/// </summary>
public static class RampEdges
{
    public static PartGeometry Build(IReadOnlyList<Vector3> deckLine, float width, LodLevel lod, bool rails = true)
    {
        ArgumentNullException.ThrowIfNull(deckLine);
        if (deckLine.Count < 2) throw new ArgumentException("A ramp edge needs a deck line.", nameof(deckLine));
        if (!(width >= 2 * S.RampCurbWidth + 0.6f && width <= 30)) throw new ArgumentOutOfRangeException(nameof(width));
        for (var i = 0; i + 1 < deckLine.Count; i++)
        {
            var delta = new Vector2(deckLine[i + 1].X - deckLine[i].X, deckLine[i + 1].Z - deckLine[i].Z);
            var run = (float)Math.Sqrt(VectorMath.Dot(delta, delta));
            if (!(run > 0.05f) || Math.Abs(deckLine[i + 1].Y - deckLine[i].Y) > 0.2f * run)
                throw new ArgumentException("Ramp segments must be at least 5 cm long and no steeper than 1:5.", nameof(deckLine));
        }
        var detail = StepsAlong.DetailOf(lod);
        var mesh = new MeshBuilder();
        float half = width * 0.5f, curb = S.RampCurbWidth, bevel = detail == Detail.Full ? S.RailBevel : 0;
        foreach (var side in new[] { -1f, 1f })
        {
            var (lat0, lat1) = side < 0 ? (-half, -half + curb) : (half - curb, half);
            var (points, chamfer) = Section(lat0, lat1, -0.2f, S.RampCurbRise, bevel);
            mesh.SweepPath(S.Trim, points, chamfer, null, deckLine, false, Frame.Identity, VertexFlag.NoShadow);
        }
        if (rails && detail != Detail.Mass)
        {
            var path = Trim(deckLine, 0.08f);
            var circle = Profiles.Circle(S.HandrailRadius, detail == Detail.Full ? S.HandrailSegments : 8).Points;
            var knob = new Vector3(S.HandrailKnob, S.HandrailKnob, S.HandrailKnob);
            foreach (var side in new[] { -1f, 1f })
            {
                var centre = side * (half - curb * 0.5f);
                var tube = circle.Select(p => new Vector2(p.X + centre, p.Y + S.HandrailHeight)).ToList();
                mesh.SweepPath(S.MetalTube, tube, null, null, path, false, Frame.Identity, VertexFlag.NoShadow, smooth: true);
                if (detail == Detail.Coarse) continue;
                foreach (var (point, lateral, _) in new[] { Station(path, 0), Station(path, float.MaxValue) })
                    mesh.Ellipsoid(S.MetalTube, Offset(point, lateral, centre, S.HandrailHeight), knob, 3, 8, Frame.Identity, VertexFlag.NoShadow);
                var total = PlanLength(path);
                var count = Math.Max(2, (int)Math.Ceiling(total / S.HandrailPostPitch - 1e-4) + 1);
                var size = S.HandrailPostSize * 0.5f;
                for (var i = 0; i < count; i++)
                {
                    var (point, lateral, tangent) = Station(path, 0.06f + (total - 0.12f) * i / (count - 1));
                    var foot = Offset(point, lateral, centre, 0);
                    var frame = Frame.Along(new(foot.X, 0, foot.Z), tangent);
                    mesh.Box(S.Metal, new(-size, point.Y + S.RampCurbRise - S.Joint, -size), new(size, point.Y + S.HandrailHeight, size),
                        detail == Detail.Full ? 0.008f : 0, frame, VertexFlag.NoShadow);
                }
            }
        }
        return mesh.Finish();
    }

    /// <summary>Rectangle lat0..lat1 × up0..up1 with single chamfers on every corner, as points and chamfer flags.</summary>
    private static (List<Vector2> Points, List<bool> Chamfer) Section(float lat0, float lat1, float up0, float up1, float bevel)
    {
        if (bevel <= 0) return ([new(lat0, up0), new(lat1, up0), new(lat1, up1), new(lat0, up1)], [false, false, false, false]);
        return ([new(lat0 + bevel, up0), new(lat1 - bevel, up0), new(lat1, up0 + bevel), new(lat1, up1 - bevel),
                 new(lat1 - bevel, up1), new(lat0 + bevel, up1), new(lat0, up1 - bevel), new(lat0, up0 + bevel)],
                [false, true, false, true, false, true, false, true]);
    }

    private static Vector3 Offset(Vector3 point, Vector2 lateral, float by, float up) => new(point.X + lateral.X * by, point.Y + up, point.Z + lateral.Y * by);

    private static float PlanLength(IReadOnlyList<Vector3> path)
    {
        float total = 0;
        for (var i = 0; i + 1 < path.Count; i++)
        {
            var delta = new Vector2(path[i + 1].X - path[i].X, path[i + 1].Z - path[i].Z);
            total += (float)Math.Sqrt(VectorMath.Dot(delta, delta));
        }
        return total;
    }

    /// <summary>Point at a plan distance along the path, with that segment's lateral (dz, −dx) and plan tangent.</summary>
    private static (Vector3 Point, Vector2 Lateral, Vector3 Tangent) Station(IReadOnlyList<Vector3> path, float distance)
    {
        for (var i = 0; i + 1 < path.Count; i++)
        {
            var a = path[i]; var b = path[i + 1];
            var d = new Vector2(b.X - a.X, b.Z - a.Z); var length = (float)Math.Sqrt(VectorMath.Dot(d, d));
            if (distance > length && i + 2 < path.Count) { distance -= length; continue; }
            var t = Math.Clamp(distance / length, 0, 1);
            return (new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, a.Z + (b.Z - a.Z) * t), new(d.Y / length, -d.X / length), new(d.X, 0, d.Y));
        }
        throw new ArgumentException("A path needs two points.", nameof(path));
    }

    /// <summary>Shortens the first and last segments by <paramref name="by"/> in plan, keeping the grade.</summary>
    private static IReadOnlyList<Vector3> Trim(IReadOnlyList<Vector3> path, float by)
    {
        var trimmed = path.ToArray();
        Vector3 Toward(Vector3 from, Vector3 to)
        {
            var delta = new Vector2(to.X - from.X, to.Z - from.Z);
            var length = (float)Math.Sqrt(VectorMath.Dot(delta, delta));
            if (!(length > 2 * by)) throw new ArgumentException("A ramp's end segments are too short for its handrails.", nameof(path));
            var t = by / length;
            return new(from.X + (to.X - from.X) * t, from.Y + (to.Y - from.Y) * t, from.Z + (to.Z - from.Z) * t);
        }
        trimmed[0] = Toward(path[0], path[1]);
        trimmed[^1] = Toward(path[^1], path[^2]);
        return trimmed;
    }
}
