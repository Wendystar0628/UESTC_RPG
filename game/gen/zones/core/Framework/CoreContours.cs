using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Gen.Zones.Common;
using Campus.Gen.Zones.Core.Library;

namespace Campus.Gen.Zones.Core.Framework;

/// <summary>Connects authored contour selections to the shell's exposed wall spans.</summary>
internal static class CoreContours
{
    public static WallPiece? Wall(IWorldModel world, string selection, IEnumerable<WallPiece> pieces)
    {
        var arc = world.ContourSegment(selection);
        return pieces.SingleOrDefault(piece => Enumerable.Range(0, arc.Length - 1).Any(i =>
            LibraryPlan.Overlap(arc[i], arc[i + 1], piece.A, piece.B) is { } span
            && span.Hi - span.Lo >= piece.Length - 0.08f));
    }

    public static int Edge(IWorldModel world, string selection, IReadOnlyList<Vector2> ring)
    {
        var arc = world.ContourSegment(selection);
        return Enumerable.Range(0, ring.Count).Single(i =>
            Vector2.DistanceSquared(ring[i], arc[0]) < 0.0001f
            && Vector2.DistanceSquared(ring[(i + 1) % ring.Count], arc[^1]) < 0.0001f);
    }
}
