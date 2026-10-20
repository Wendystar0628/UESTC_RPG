using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Parts;
using S = Campus.Gen.Furniture.FurnitureStyle;
using static Campus.Gen.Furniture.FurniturePlacement;
using static Campus.Gen.Furniture.FurnitureParts;
using static Campus.Gen.Furniture.FurnitureSlots;

namespace Campus.Gen.Furniture;

/// <summary>Places a road rest bench and its bin along the same walk edge.</summary>
internal static class FurnitureRestStops
{
    /// <summary>
    /// The rest stop at arc length <paramref name="s"/> of the road, on <paramref name="side"/>: a bench lining the nearest walk on that
    /// side (within <see cref="FurnitureStyle.RestReach"/> of the centre line, sliding along it by up to <see cref="FurnitureStyle.RestSlide"/>),
    /// its front <see cref="FurnitureStyle.WalkGap"/> off the walk's edge and facing it, on a bay joined to the walk. The walk's road
    /// side is tried first (back to the trees and the road, looking away from it); the far side only takes a bench where no
    /// carriageway lies <see cref="FurnitureStyle.FaceClear"/> ahead. With <paramref name="withBin"/>, a bin on its own bay along the
    /// same edge, <see cref="FurnitureStyle.BinGaps"/> clear of the bench, on the side nearer another walk (a junction).
    /// </summary>
    internal static (bool Bench, bool Bin) Place(StreetSite site, List<FurnitureItem> items, StreetNetwork.Run run, float s, float side, float half, bool withBin, string key)
    {
        if (run.Normal(s) is not { } normal) return (false, false);
        var road = run.At(s);
        var outward = normal * side;
        foreach (var walk in site.Paving.Walks(road, S.RestReach))
        {
            // The walk must lie on this side, beyond the carriageway, and stay wide enough to pass a sitter's feet.
            var whalf = StreetNetwork.HalfWidth(walk.Line);
            if (Vector2.Dot(walk.Point - road, outward) <= half || 2f * whalf - S.FeetReach < S.PassClear) continue;
            var path = new StreetNetwork.Run([.. walk.Line.Line.Points]);
            var ends = S.Bench.Width * 0.5f + S.ItemGap;
            foreach (var shift in Shifts(S.RestSlide))
            {
                var at = walk.Along + shift;
                if (at < ends || at > path.Length - ends || path.Normal(at) is not { } n) continue;
                var roadSide = Vector2.Dot(n, outward) < 0f ? 1f : -1f;
                foreach (var bside in new[] { roadSide, -roadSide })
                {
                    if (WalkSlot(site, path, at, bside, walk.Line, S.Bench, facesWalk: true) is not { } bench) continue;
                    items.Add(Fitted(key + ":bench", FurnitureKind.Bench, PartCatalog.Bench, S.Bench, bench, BenchDef.Collision(S.Bench)));
                    site.Occupy(bench.Ends.A, bench.Ends.B, bench.Ends.R);
                    return (true, withBin && PlaceBin(site, items, path, at, bside, walk.Line, key + ":bin"));
                }
            }
        }
        return (false, false);
    }

    /// <summary>A bin along the same walk edge as the bench at <paramref name="benchAt"/>, 2–4 m clear of it, nearer side to a junction first.</summary>
    private static bool PlaceBin(StreetSite site, List<FurnitureItem> items, StreetNetwork.Run path, float benchAt, float bside, NetworkLine walk, string key)
    {
        var centre = S.Bench.Width * 0.5f + S.Bin.Width * 0.5f;
        float Junction(float sign) => site.Paving.OtherWalk(path.At(benchAt + sign * (centre + S.BinGaps[0])), walk);
        var first = Junction(-1f) < Junction(1f) ? -1f : 1f;
        foreach (var sign in new[] { first, -first })
            foreach (var gap in S.BinGaps)
            {
                var at = benchAt + sign * (centre + gap);
                if (at < S.Bin.Width * 0.5f || at > path.Length - S.Bin.Width * 0.5f) continue;
                if (WalkSlot(site, path, at, bside, walk, S.Bin, facesWalk: false) is not { } bin) continue;
                items.Add(Fitted(key, FurnitureKind.Bin, PartCatalog.BinFour, S.Bin, bin, BinDef.Collision(S.Bin)));
                site.Occupy(bin.Ends.A, bin.Ends.B, bin.Ends.R);
                return true;
            }
        return false;
    }
}
