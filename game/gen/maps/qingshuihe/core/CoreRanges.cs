using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.Sites;
using Campus.Gen.Maps.Qingshuihe.Core.Annex;
using Campus.Gen.Maps.Qingshuihe.Core.Axis;
using Campus.Gen.Maps.Qingshuihe.Core.Entrance;
using S = Campus.Gen.Maps.Qingshuihe.Core.CoreStyle;

namespace Campus.Gen.Maps.Qingshuihe.Core;

/// <summary>Source envelopes for the core's pieces, including their trim, approaches and ground-read footprint.</summary>
internal static class CoreRanges
{
    public static IReadOnlyList<MapRect> All(IWorldModel world)
    {
        var ranges = CoreSite.Claims.OfType<FeatureClaim>()
            .Where(claim => claim.Feature != CoreIds.PlazaRingSteps)
            .Select(claim => ForFeature(world, claim.Feature)).OfType<MapRect>().ToList();
        if (Garden(world) is { } garden) ranges.Add(garden);
        return ranges;
    }

    public static MapRect? ForFeature(IWorldModel world, FeatureId id)
    {
        if (!world.Features.TryGetValue(id, out var feature)) return null;
        if (id == CoreIds.TimePlaza)
        {
            var (centre, radius) = TimePlaza.Plan(feature);
            var reach = radius + MathF.Max(PlazaStyle.RimLampOffset + PlazaStyle.RimLampWidth, PlazaStyle.RampWidth + 2 * PlazaStyle.CheekWidth);
            var bounds = SiteRanges.Point(centre, reach);
            var bench = TimePlaza.BenchParameters;
            var benchReach = MathF.Sqrt(bench.Width * bench.Width + bench.Depth * bench.Depth) * 0.5f;
            var benches = world.Features.Values
                .Where(item => item.Id.Value.StartsWith(CoreIds.PlazaBenchPrefix, StringComparison.Ordinal) && item.Geometry is PointGeometry)
                .Select(item => SiteRanges.Bounds(item, benchReach));
            return Union(benches.Prepend(bounds));
        }

        var padding = id == CoreIds.Sundial ? PlazaStyle.SundialPadRadius
            : id == CoreIds.Library ? LibraryStyle.CafeApronDepth + LibraryStyle.CafeWalkReach + LibraryStyle.CafeWalkWidth * 0.5f + LibraryStyle.CafeWalkOverlap
            : id == CoreIds.LibraryStair ? GrandStair.HalfWidth + EntranceStyle.CanopyDepth + LibraryStyle.CopingOverhang
            : id == CoreIds.Museum ? MuseumStyle.PorticoOuter + MuseumStyle.PorticoProject + MuseumStyle.PorticoColumnBase
            : AnnexStyle.AnnexCanopyDepth + LibraryStyle.CopingOverhang;
        return SiteRanges.Bounds(feature, padding);
    }

    public static MapRect? Garden(IWorldModel world)
    {
        if (!world.Features.ContainsKey(CoreIds.CourtWing)) return null;
        // The court is the hole enclosed by these raw building outlines; no shell or courtyard search is needed here.
        var ids = AnnexSite.Members.Select(member => member.Feature).Prepend(CoreIds.Library)
            .Append(CoreIds.NorthEastConnector).Append(CoreIds.NorthEastBridge).Append(CoreIds.CafeStore);
        var ranges = ids.Where(world.Features.ContainsKey)
            .Select(id => SiteRanges.Bounds(world.Features[id], AnnexStyle.PavilionOverhang)).ToArray();
        return ranges.Length == 0 ? null : Union(ranges);
    }

    private static MapRect Union(IEnumerable<MapRect> ranges) =>
        SiteRanges.Bounds(ranges.SelectMany(range => new[] { range.Min, range.Max }));
}
