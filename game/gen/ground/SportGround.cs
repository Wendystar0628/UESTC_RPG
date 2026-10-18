using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Gen.World;

namespace Campus.Gen.Ground;

/// <summary>
/// Sports grounds read from their source geometry (WP-15). A running track is mapped as the closed line along its inner
/// edge, so it is laid as a ring of <see cref="TrackWidth"/> outside that line: the pitch and the lawn of the infield stay
/// inside it. A football pitch is a quadrilateral whose rectangle <see cref="FitPitch"/> recovers for the markings.
/// Everything here works on tags and geometry only, never on entity IDs.
/// </summary>
internal static class SportGround
{
    /// <summary>Width of a track laid as a ring: six to seven 1.22 m lanes.</summary>
    public const float TrackWidth = 8f;

    /// <summary>Sharpest turn a mitred offset may take: 1 + cos of the turn must stay above this (about 130 degrees).</summary>
    private const float MiterLimit = 0.35f;

    /// <summary>
    /// A track polygon without holes becomes a ring: the polygon itself is the inner edge and its outward offset the outer
    /// edge. A polygon that already has holes is a ring the author drew, and a polygon that cannot be offset (concave or
    /// too sharp) becomes a band centred on its outline.
    /// </summary>
    public static GroundShape? TrackRing(FeatureId id, PolygonRings rings)
    {
        if (rings.Holes.Count > 0) return GroundSources.Polygon(id, SurfaceKind.Track, rings);
        var inner = PlanGeometry.CleanPath(rings.Outer);
        if (inner.Length < 3) return null;
        if (Outward(inner, TrackWidth) is { } outer)
            return GroundSources.Polygon(id, SurfaceKind.Track, new PolygonRings(outer, [inner]));
        return Band(id, [.. inner, inner[0]], TrackWidth);
    }

    /// <summary>A track mapped as a line is a band of <paramref name="width"/> (else <see cref="TrackWidth"/>) along it; a closed line stays closed.</summary>
    public static GroundShape? TrackLine(FeatureId id, IReadOnlyList<Vector2> line, float? width)
    {
        var points = PlanGeometry.CleanPath(line);
        if (points.Length < 2) return null;
        var closed = line.Count > 2 && Vector2.DistanceSquared(line[0], line[^1]) < 1e-8f;
        if (closed && points.Length > 2) points = [.. points, points[0]];
        return Band(id, points, width is > 0f ? width.Value : TrackWidth);
    }

    private static GroundShape Band(FeatureId id, Vector2[] line, float width)
    {
        var half = width * 0.5f;
        var min = line[0];
        var max = line[0];
        foreach (var p in line) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
        return new GroundShape { Kind = SurfaceKind.Track, Id = id, Line = line, HalfWidth = half, Min = min - new Vector2(half), Max = max + new Vector2(half) };
    }

    /// <summary>Mitred outward offset of a convex ring; null when the ring is concave, degenerate or turns too sharply.</summary>
    internal static Vector2[]? Outward(Vector2[] ring, float distance)
    {
        var n = ring.Length;
        var area = PlanGeometry.SignedArea(ring);
        if (Math.Abs(area) < 5e-4) return null;
        var side = area > 0 ? 1f : -1f;   // +1: counter-clockwise in (x, z); the outside is then on the right of every edge
        var direction = new Vector2[n];
        var normal = new Vector2[n];
        for (var i = 0; i < n; i++)
        {
            var d = ring[(i + 1) % n] - ring[i];
            var length = d.Length();
            if (!(length > 1e-4f)) return null;
            direction[i] = d / length;
            normal[i] = side > 0f ? new(direction[i].Y, -direction[i].X) : new(-direction[i].Y, direction[i].X);
        }
        var result = new Vector2[n];
        for (var i = 0; i < n; i++)
        {
            var before = (i + n - 1) % n;
            var turn = direction[before].X * direction[i].Y - direction[before].Y * direction[i].X;
            if (turn * side < -1e-4f) return null;
            var k = 1f + Vector2.Dot(normal[before], normal[i]);
            if (k < MiterLimit) return null;
            result[i] = ring[i] + (normal[before] + normal[i]) * (distance / k);
        }
        return result;
    }

    /// <summary>
    /// The rectangle of a football pitch: its centre, the unit vector along the touchlines, the one across, and the side
    /// lengths. Null unless the shape is a plain quadrilateral that hugs such a rectangle, is a plausible football
    /// proportion and is not too small to mark.
    /// </summary>
    public static PitchFrame? FitPitch(GroundShape shape, PitchSpec spec)
    {
        if (shape.Rings is not { Length: 1 } rings || rings[0].Length != 4) return null;
        var c = rings[0];
        Vector2[] edges = [c[1] - c[0], c[2] - c[1], c[3] - c[2], c[0] - c[3]];
        var first = (edges[0].Length() + edges[2].Length()) * 0.5f;
        var second = (edges[1].Length() + edges[3].Length()) * 0.5f;
        var axis = first >= second ? edges[0] - edges[2] : edges[1] - edges[3];
        if (!(axis.Length() > 1e-3f)) return null;
        var length = MathF.Max(first, second);
        var width = MathF.Min(first, second);
        if (!(width >= spec.MinShortSide) || length / width < spec.MinAspect || length / width > spec.MaxAspect) return null;
        var along = Vector2.Normalize(axis);
        var across = new Vector2(-along.Y, along.X);
        var centre = (c[0] + c[1] + c[2] + c[3]) * 0.25f;
        // Every corner must lie near a corner of the fitted rectangle, or the quadrilateral is a trapezoid or a kite.
        var tolerance = MathF.Max(0.75f, 0.02f * length);
        foreach (var corner in c)
        {
            var offset = corner - centre;
            var u = MathF.Abs(Vector2.Dot(offset, along));
            var v = MathF.Abs(Vector2.Dot(offset, across));
            if (MathF.Abs(u - length * 0.5f) > tolerance || MathF.Abs(v - width * 0.5f) > tolerance) return null;
        }
        return new PitchFrame(centre, along, across, length, width);
    }
}

/// <summary>A pitch's rectangle in plan: centre, unit vectors along and across, and the side lengths in metres.</summary>
internal readonly record struct PitchFrame(Vector2 Centre, Vector2 Along, Vector2 Across, float Length, float Width);
