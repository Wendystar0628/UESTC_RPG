using Campus.Contracts;
using Campus.Contracts.Bake;
using N = System.Numerics;

namespace Campus.Systems.Fauna;

/// <summary>Pure owner-tile demand and visibility shared by streaming and the display latch.</summary>
public static class FaunaDemand
{
    public static StreamTileNeed Evaluate(in BakeTileEntry entry, in StreamDemandView view, bool resident)
    {
        foreach (var near in view.Near.Span)
            if (entry.Bounds.Intersects(near)) return StreamTileNeed.Required;
        // Animals have no far-distance cutoff: actual main and reflected views request distant sites.
        return InView(entry.Bounds, view.Frustum.Span) ? StreamTileNeed.Required : StreamTileNeed.None;
    }

    /// <summary>Outward plane convention shared by the director and the real reflection bridge.</summary>
    internal static bool InView(Box3 box, ReadOnlySpan<N.Plane> planes)
    {
        if (planes.IsEmpty) return false;
        foreach (var plane in planes)
        {
            var n = plane.Normal;
            var corner = new N.Vector3(n.X > 0 ? box.Min.X : box.Max.X,
                n.Y > 0 ? box.Min.Y : box.Max.Y, n.Z > 0 ? box.Min.Z : box.Max.Z);
            if (N.Vector3.Dot(n, corner) + plane.D > 0) return false;
        }
        return true;
    }
}
