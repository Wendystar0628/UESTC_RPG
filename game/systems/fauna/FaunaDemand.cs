using Campus.Contracts;
using Campus.Contracts.Bake;
using N = System.Numerics;

namespace Campus.Systems.Fauna;

/// <summary>Pure owner-tile demand and visibility shared by streaming and the display latch.</summary>
public static class FaunaDemand
{
    public static StreamTileNeed Evaluate(in BakeTileEntry entry, in StreamDemandView view, bool resident)
    {
        if (view.ResourcePreload) return StreamTileNeed.None;
        foreach (var near in view.Near.Span)
            if (entry.Bounds.Intersects(near)) return StreamTileNeed.Required;
        // Animals have no far-distance cutoff: actual main and reflected views request distant sites.
        return entry.Bounds.Intersects(view.Frustum.Span) ? StreamTileNeed.Required : StreamTileNeed.None;
    }

}
