using Campus.Contracts;
using Campus.Contracts.Water;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Street;
using W = Campus.Contracts.Water.WaterPoolRules;
using P = Campus.Contracts.Water.WaterPalette;

namespace Campus.Gen.Parts.Sport;

/// <summary>Tile shell and lane stripes share the water/terrain depth profile; origin is the water surface at the authored axis start.</summary>
public static class PoolBasin
{
    private static readonly ToonSpec Tile = new(P.PoolTileLit, P.PoolTileShade) { Line = LineClass.None };
    private static readonly ToonSpec Stripe = new(P.PoolLineLit, P.PoolLineShade) { Line = LineClass.None };

    public static PartGeometry Build(float length, float width)
    {
        var b = new PartBuilder();
        var stations = new[] { 0, length * W.ShallowFraction, length };
        var lanes = W.LaneCount(width);
        for (var band = 0; band + 1 < stations.Length; band++)
        {
            var start = stations[band]; var end = stations[band + 1]; var mid = (start + end) * 0.5f;
            var depth = W.DepthAt(mid, length);
            b.Add(PartSolids.BeveledBox(end - start, SportsStyle.Slab, width, SportsStyle.Bevel, Tile),
                Xf.Move(mid, -depth - SportsStyle.Slab, 0));
            foreach (var side in new[] { -1f, 1f })
                b.Add(PartSolids.BeveledBox(end - start, depth + W.RimDrop, SportsStyle.Slab, SportsStyle.Bevel, Tile),
                    Xf.Move(mid, -depth, side * width * 0.5f));
            for (var lane = 0; lane < lanes; lane++)
            {
                var across = W.LaneCenter(lane, lanes);
                var lineStart = MathF.Max(start, W.LaneEndInset);
                var lineEnd = MathF.Min(end, length - W.LaneEndInset);
                if (lineEnd > lineStart)
                    b.Add(PartSolids.BeveledBox(lineEnd - lineStart, StreetStyle.PrintLift, W.LineWidth, 0, Stripe),
                        Xf.Move((lineStart + lineEnd) * 0.5f, -depth + StreetStyle.PrintLift, across), VertexFlag.NoShadow);
            }
        }
        foreach (var endBar in new[] { W.LaneEndInset, length - W.LaneEndInset })
            for (var lane = 0; lane < lanes; lane++)
                b.Add(PartSolids.BeveledBox(W.LineWidth, StreetStyle.PrintLift, W.LaneEndBarLength, 0, Stripe),
                    Xf.Move(endBar, -W.DepthAt(endBar, length) + StreetStyle.PrintLift, W.LaneCenter(lane, lanes)), VertexFlag.NoShadow);
        foreach (var x in new[] { 0f, length })
        {
            var depth = W.DepthAt(x, length);
            b.Add(PartSolids.BeveledBox(SportsStyle.Slab, depth + W.RimDrop, width, SportsStyle.Bevel, Tile), Xf.Move(x, -depth, 0));
        }
        // The physical 0.6 m change is the same band boundary that terrain and water presentation consume.
        b.Add(PartSolids.BeveledBox(SportsStyle.Slab, W.DeepDepth - W.ShallowDepth, width, SportsStyle.Bevel, Tile),
            Xf.Move(length * W.ShallowFraction, -W.DeepDepth, 0));
        var (min, max) = b.Measure();
        return b.Finish(new(min, max));
    }
}
