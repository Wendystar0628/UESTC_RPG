using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Contracts.Water;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Sport;
using Campus.Gen.World;
using Campus.Gen.Sites;
using Campus.Gen.Maps.Qingshuihe.CenterEast.Framework;
using S = Campus.Gen.Parts.Sport.SportsStyle;
using L = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Look;

namespace Campus.Gen.Maps.Qingshuihe.CenterEast.Sports;

/// <summary>Pool coping, ladders and locked enclosure; water sheets and traversal stay with WP-24.</summary>
public sealed class PoolSite : ZoneSite
{
    public override string Id => "21d.pool-enclosure";

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) =>
        [SiteRanges.Bounds(world.Feature(CenterEastIds.Sports.PoolEnclosure.Entity), S.PoolGateWidth + S.FenceGroundBand),
         .. CenterEastIds.Sports.Pools.Select(pool => SiteRanges.Bounds(world.Feature(pool.Entity), S.Cover + S.Rail))];
    private static IEnumerable<(Vector2 A, Vector2 B, float Bottom, float Top, bool Closed)> Runs(IWorldModel world) =>
        CourtSites.FenceRuns(world.Feature(CenterEastIds.Sports.PoolEnclosure.Entity).Contour(), world.Feature(CenterEastIds.Sports.PoolGate).Point(), S.PoolFence, S.PoolGateWidth, true);

    public override void Build(SiteBuilder b)
    {
        foreach (var (a, end, bottom, top, locked) in Runs(b.Context.World))
        {
            if (!b.Includes(SiteRanges.Bounds([a, end], S.Post * 3))) continue;
            var length = Vector2.Distance(a, end);
            if (length > S.Post) b.Add(SportsParts.Fence(length, top - bottom, b.Detail == SiteDetail.Full, locked), PlanFrame.Toward(a, end).At(0, b.Ground(a) + bottom, 0));
        }
        foreach (var pool in CenterEastIds.Sports.Pools)
        {
            var feature = b.Context.World.Feature(pool.Entity);
            if (!b.Includes(SiteRanges.Bounds(feature, S.Cover + S.Rail))) continue;
            var ring = feature.Contour();
            var waterY = b.Context.World.Terrain.WaterY(feature.Id)
                ?? throw new InvalidOperationException($"Pool {feature.Id.Value} has no terrain water level.");
            var y = waterY + WaterPoolRules.RimDrop;
            b.Add(Contour.Wall(ring, S.Slab, y - S.Slab, y, L.Granite), Matrix4x4.Identity);
            var poolFrame = WaterPoolLayout.Frame(feature);
            var frame = PlanFrame.Along(poolFrame.Origin, poolFrame.Along);
            b.Add(PoolBasin.Build(poolFrame.Length, poolFrame.Width), frame.At(0, waterY, 0));
            if (b.Detail != SiteDetail.Mass)
                foreach (var along in new[] { poolFrame.Length * 0.25f, poolFrame.Length * 0.75f })
                    b.Add(SportsParts.PoolLadder(b.Detail == SiteDetail.Full), frame.AtReversed(along, y, -poolFrame.Width * 0.5f));
        }
    }

    public override void Collide(SiteColliders c)
    {
        foreach (var (a, b, bottom, top, locked) in Runs(c.Context.World))
            if (c.Includes(SiteRanges.Bounds([a, b], S.FenceGroundBand)) && Vector2.Distance(a, b) > S.Post)
            {
                var ground = SportsLayout.FenceGround(c.Context.World, a, b);
                c.Wall(a, b, S.Post, ground.Low + bottom, ground.High + top, locked ? SurfaceTag.DoorClosed : SurfaceTag.Prop, FootMat.Metal);
            }
    }
}

/// <summary>Two closed service houses retain the ordinary block vocabulary with pool/mechanical window cards.</summary>
public sealed class PoolRoomsSite : ZoneSite
{
    public override string Id => "21d.pool-rooms";

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) =>
        [.. Entities.Select(id => SiteRanges.Bounds(world.Feature(id), S.Cover + S.Slab))];
    private static readonly string[] Entities = [CenterEastIds.Sports.PoolLargeRoom.Entity, CenterEastIds.Sports.PoolSmallRoom.Entity];
    public override IReadOnlyList<Claim> Claims => Entities.Select(id => (Claim)new FeatureClaim(new(id), Aspects.Massing | Aspects.Facade | Aspects.Roof)).ToArray();
    // Source traces are per house because one site can carry more than one claim.
    public override IEnumerable<ContentSourceTrace> TraceSources(BuildContext ctx, LodLevel lod)
    {
        foreach (var id in Entities)
            yield return SiteSourceTrace.Build(ctx, lod, Id + "." + id, new FeatureId(id), b => BuildRoom(b, id), c => CollideRoom(c, id), [new(ctx.World.Feature(id).Contour())]);
    }
    private static GridEdge Dress(GridEdgeInfo edge) => new(EdgeKind.Plain);
    private static readonly GridStyle DefaultStyle = new(L.Wall, L.LightTrim, L.DarkGranite, ZoneLooks.Glass, L.Metal, L.RoofSlab);
    private static GridStyle Style(WorldFeature feature) => DefaultStyle with
    {
        Storeys = feature.Storeys!.Value,
        GroundStorey = feature.Height!.Value / feature.Storeys.Value - S.Slab * 2,
        Storey = feature.Height.Value / feature.Storeys.Value,
        GroundWindow = feature.Height.Value / feature.Storeys.Value - DefaultStyle.GroundSill - S.Slab * 2,
        Window = feature.Height.Value / feature.Storeys.Value - S.Cover * 2,
        Cornice = S.Slab, Parapet = S.Slab,
    };
    private static GridNumbering Numbering(string id) => new(new(id), "service", 0, new("window.gym"), id == CenterEastIds.Sports.PoolLargeRoom.Entity ? (byte)14 : (byte)15);
    public override IEnumerable<(FeatureId Feature, WindowGridSpec Grid)> WindowGrids(IWorldModel world)
    {
        foreach (var id in Entities)
        {
            var f = world.Feature(id); var layout = GridFacade.Layout(f.Contour(), Style(f), Dress);
            yield return (new(id), new("service", layout.Columns, layout.Rows));
        }
    }
    public override void DescribeWindows(BuildContext ctx, ICollection<WindowFacadeDecl> facades, ICollection<WindowDecl> windows)
    {
        foreach (var id in Entities)
        {
            var f = ctx.World.Feature(id);
            GridFacade.Declare(facades, f.Contour(), ctx.World.Terrain.PadY(f.Id), Style(f), Dress, Numbering(id), WindowNumbers.Required(ctx, f.Id, "service"));
        }
    }
    public override void Build(SiteBuilder b) { foreach (var id in Entities.Where(id => b.Includes(SiteRanges.Bounds(b.Context.World.Feature(id), S.Cover + S.Slab)))) BuildRoom(b, id); }
    private static void BuildRoom(SiteBuilder b, string id)
    {
        var f = b.Context.World.Feature(id);
        GridFacade.Build(b, f.Contour(), b.Context.World.Terrain.PadY(f.Id), Style(f), Dress, Numbering(id));
        if (b.Detail != SiteDetail.Mass)
        {
            var edge = GridFacade.Edges(f.Contour()).OrderByDescending(e => e.Info.Length).First().Info;
            var door = (DoorParams)Campus.Contracts.Parts.PartCatalog.Find(new(Campus.Contracts.Parts.PartCatalog.DoorFire))!.Defaults;
            b.Place(Campus.Contracts.Parts.PartCatalog.DoorFire, door, edge.Frame.AtReversed(edge.Length * 0.5f, b.Context.World.Terrain.PadY(f.Id), S.Cover));
        }
    }
    public override void Collide(SiteColliders c) { foreach (var id in Entities.Where(id => c.Includes(SiteRanges.Bounds(c.Context.World.Feature(id), S.Cover + S.Slab)))) CollideRoom(c, id); }
    private static void CollideRoom(SiteColliders c, string id)
    {
        var f = c.Context.World.Feature(id);
        GridFacade.Collide(c, f.Contour(), c.Context.World.Terrain.PadY(f.Id), Style(f), Dress, FootMat.Concrete);
    }
}
