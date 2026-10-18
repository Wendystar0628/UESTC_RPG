using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Gen.Zones.Common;
using Campus.Gen.Zones.DesignWaterfront.Bridges;
using T = Campus.Gen.Zones.DesignWaterfront.WaterfrontTune;

namespace Campus.Gen.Zones.DesignWaterfront;

/// <summary>WP-28 registered bridge decks and bridge-head stones, with their network lines kept intact.</summary>
[ContentModule("zone.waterfront", Owner = "WP-28")]
public sealed class WaterfrontModule : ZoneModuleBase
{
    private static readonly IReadOnlyList<ZoneSite> Sites = [new BridgeSite()];

    public WaterfrontModule() : base(Sites, new(T.BudgetTrianglesL0, T.BudgetTrianglesL1, T.BudgetDrawCalls),
        cacheColliders: true) { }

    public override ContentOutput Build(BuildContext context, TileKey tile, LodLevel lod)
    {
        var output = base.Build(context, tile, lod);
        var occupancy = BridgePlan.All(context.World)
            .Where(plan => lod == LodLevel.L4 || TileGrid.At(plan.Centre, lod) == tile)
            .Select(plan => new Footprint2D(plan.Id, new PolygonRings(plan.Footprint),
                plan.RoadY - T.SlabDepth, plan.WalkY + T.HeadTop)).ToArray();
        return occupancy.Length == 0 ? output : output with { Occupancy = occupancy };
    }
}

internal sealed class BridgeSite : ZoneSite
{
    public override string Id => "28a.bridges";

    public override void Build(SiteBuilder builder)
    {
        foreach (var plan in BridgePlan.All(builder.Context.World))
            if (BridgeMesher.Build(plan, builder.Lod) is { } geometry)
                builder.Add(geometry, Matrix4x4.Identity, plan.Centre);
        foreach (var stone in BridgeStone.All(builder.Context.World))
            if (stone.Build(builder.Lod) is { } geometry)
                builder.Add(geometry, Matrix4x4.Identity, stone.Position);
    }

    public override void Collide(SiteColliders colliders)
    {
        foreach (var plan in BridgePlan.All(colliders.Context.World))
            foreach (var (anchor, collider) in BridgeColliders.Of(plan)) colliders.Add(collider, anchor);
        foreach (var stone in BridgeStone.All(colliders.Context.World)) colliders.Add(stone.Collider, stone.Position);
    }
}
