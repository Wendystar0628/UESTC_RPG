using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.Geometry;
using Campus.Gen.Parts.Core;
using Campus.Gen.Parts.Sport;
using Campus.Gen.Sites;
using Campus.Gen.Maps.Qingshuihe.CenterEast.Framework;
using S = Campus.Gen.Parts.Sport.SportsStyle;
using L = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Look;

namespace Campus.Gen.Maps.Qingshuihe.CenterEast.Sports;

/// <summary>Silver barrel hall plus the source annexes; the open interior remains game/gen/maps/qingshuihe/west/SixInterior/'s.</summary>
public sealed class TrainingHallSite : ZoneSite
{
    public override string Id => "21d.training-hall";

    public override IReadOnlyList<Campus.Contracts.Maps.MapRect> Ranges(IWorldModel world) =>
        [SiteRanges.Bounds(world.Feature(CenterEastIds.Sports.Training.Entity), S.Cover)];
    public override IReadOnlyList<Claim> Claims => [new FeatureClaim(new(CenterEastIds.Sports.Training.Entity), Aspects.Massing | Aspects.Facade | Aspects.Roof)];
    private static readonly ChannelId Channel = new("window.gym");

    internal static GridStyle Style(WorldFeaturePart part) => new(L.Wall, L.LightTrim, L.DarkGranite, ZoneLooks.Glass, L.Metal, S.Silver)
    {
        Storeys = part.Levels().Storeys,
        GroundStorey = part.Levels().Height / part.Levels().Storeys - S.Slab * 2,
        Storey = part.Levels().Height / part.Levels().Storeys,
        Cornice = S.Slab,
        Parapet = S.Slab,
    };

    private static bool Hall(WorldFeaturePart part) => part.SourceId == CenterEastIds.Sports.Hall.Part;
    private static string Key(WorldFeaturePart part, int index) => part.SourceId + "#" + index;
    private static IEnumerable<(WorldFeaturePart Part, PolygonRings Ring, string Key)> Annexes(IWorldModel world) =>
        world.Feature(CenterEastIds.Sports.Training.Entity).Parts.Where(p => !Hall(p)).SelectMany(part =>
            WorldFeatureGeometry.PolygonsOf(WorldFeatureGeometry.FacadeGeometryOf(part)).Select((ring, i) => (part, ring, Key(part, i))));
    private static readonly DoorParams Door = (DoorParams)Campus.Contracts.Parts.PartCatalog.Find(new(Campus.Contracts.Parts.PartCatalog.DoorGlass))!.Defaults;
    private static GridEdge Dress(WorldFeaturePart part, PolygonRings ring, GridEdgeInfo edge)
    {
        if (part.SourceId != CenterEastIds.Sports.NorthAnnex.Part) return new(EdgeKind.Plain);
        var north = GridFacade.Edges(ring).OrderBy(e => (e.Info.A.Y + e.Info.B.Y) * 0.5f).First().Info;
        return edge.Index == north.Index ? new(EdgeKind.Plain) { Cuts = [(edge.Length * 0.5f - Door.Width * 0.5f, edge.Length * 0.5f + Door.Width * 0.5f, Door.Height)] } : new(EdgeKind.Plain);
    }
    private static GridNumbering Numbering(string key) => new(new(CenterEastIds.Sports.Training.Entity), key, 0, Channel, 13);

    protected override IReadOnlyList<PolygonRings> OccupiedContours(IWorldModel world, LodLevel lod) =>
        SportsLayout.TrainingParts(world).Select(p => p.Rings).ToArray();

    public override IEnumerable<(FeatureId Feature, WindowGridSpec Grid)> WindowGrids(IWorldModel world)
    {
        foreach (var (part, ring, key) in Annexes(world))
        {
            var layout = GridFacade.Layout(ring, Style(part), e => Dress(part, ring, e));
            if (layout.Columns > 0) yield return (new(CenterEastIds.Sports.Training.Entity), new(key, layout.Columns, layout.Rows));
        }
    }

    public override void DescribeWindows(BuildContext ctx, ICollection<WindowFacadeDecl> facades, ICollection<WindowDecl> windows)
    {
        var y = ctx.World.Terrain.PadY(new(CenterEastIds.Sports.Training.Entity));
        foreach (var (part, ring, key) in Annexes(ctx.World))
            if (GridFacade.Layout(ring, Style(part), e => Dress(part, ring, e)).Columns > 0)
                GridFacade.Declare(facades, ring, y, Style(part), e => Dress(part, ring, e), Numbering(key), WindowNumbers.Allocation(ctx, new(CenterEastIds.Sports.Training.Entity), key));
    }

    public override void Build(SiteBuilder b)
    {
        var world = b.Context.World;
        var feature = world.Feature(CenterEastIds.Sports.Training.Entity);
        var y = world.Terrain.PadY(feature.Id);
        foreach (var (part, ring, key) in Annexes(world))
        {
            if (!b.Includes(SiteRanges.Bounds(ring.Outer, S.Cover))) continue;
            GridFacade.Build(b, ring, y, Style(part), e => Dress(part, ring, e), GridFacade.Layout(ring, Style(part), e => Dress(part, ring, e)).Columns > 0 ? Numbering(key) : null);
            if (part.SourceId != CenterEastIds.Sports.NorthAnnex.Part) continue;
            var north = GridFacade.Edges(ring).OrderBy(e => (e.Info.A.Y + e.Info.B.Y) * 0.5f).First().Info;
            b.Place(Campus.Contracts.Parts.PartCatalog.DoorGlass, Door with { OpenAngleDeg = 90 }, north.Frame.AtReversed(north.Length * 0.5f, y, 0));
        }
        var hall = feature.Part(CenterEastIds.Sports.Hall.Part).Contour();
        if (!b.Includes(SiteRanges.Bounds(hall, S.Cover))) return;
        b.Add(Contour.Wall(hall, S.Slab, y, y + S.Spring, L.Wall), Matrix4x4.Identity);
        var r = feature.Rectangle(SportsLayout.Axis(feature), CenterEastIds.Sports.Hall.Part);
        b.Add(TrainingRoof.Build(r.Width, r.Depth, b.Detail == SiteDetail.Full), r.Frame.At(0, y, 0));
    }

    public override void Collide(SiteColliders c)
    {
        var world = c.Context.World;
        var y = world.Terrain.PadY(new(CenterEastIds.Sports.Training.Entity));
        foreach (var (part, ring, _) in Annexes(world).Where(annex => c.Includes(SiteRanges.Bounds(annex.Ring.Outer, S.Cover)))) GridFacade.Collide(c, ring, y, Style(part), e => Dress(part, ring, e), FootMat.Concrete);
        var hall = world.Feature(CenterEastIds.Sports.Training.Entity).Part(CenterEastIds.Sports.Hall.Part).Contour();
        if (!c.Includes(SiteRanges.Bounds(hall, S.Cover))) return;
        foreach (var (a, b) in SportsLayout.Edges(hall)) c.Wall(a, b, S.Slab * 0.5f, y, y + S.Spring, SurfaceTag.Wall, FootMat.Concrete);
    }
}
